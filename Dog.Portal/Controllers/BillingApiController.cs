using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Dog.Portal.Controllers
{
    /// <summary>
    /// Pass-through from the invoice / customer-company / billing-settings screens to the API. It only adds what the
    /// browser cannot: the session's access token (via ApiClient) and the active company/branch. Every permission check is
    /// still done by the API, and only these API areas can be reached through it.
    /// </summary>
    [Route("BillingApi")]
    public class BillingApiController : Controller
    {
        private static readonly Regex SafePath = new(@"^[A-Za-z0-9/_\-]+$", RegexOptions.Compiled);
        private static readonly string[] Allowed = { "Invoice/", "CustomerCompany", "BillingSetup/", "Announcements/" };
        private readonly IApiClient _api;

        public BillingApiController(IApiClient api) => _api = api;

        private UserSessionModel? Session()
        {
            var json = HttpContext.Session.GetString("UserSession");
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<UserSessionModel>(json);
        }

        private static int ActiveCompanyId(UserSessionModel s)
            => s.IsHeadOffice && s.CurrentBranchId is > 0 ? s.CurrentBranchId.Value : Convert.ToInt32(s.CompanyId);

        private IActionResult Fail(string message, int status = 200)
        {
            Response.StatusCode = status;
            return Json(new { success = false, message });
        }

        private IActionResult Relay(ApiRawResult r)
        {
            if (r.IsSuccessStatusCode) return Content(string.IsNullOrWhiteSpace(r.Body) ? "{\"success\":true}" : r.Body, "application/json");
            return r.StatusCode switch
            {
                401 => Fail("Your session has expired. Please sign in again."),
                403 => Fail("You do not have permission to do this."),
                404 => Fail("Not found."),
                _ => Fail($"The request failed ({r.StatusCode}).")
            };
        }

        private IActionResult? Guard(string path, out string endpoint)
        {
            endpoint = "";
            if (Session() == null) return Fail("Your session has expired. Please sign in again.", 401);
            path = (path ?? "").Trim('/');
            if (path.Length == 0 || path.Contains("..") || !SafePath.IsMatch(path)) return Fail("Invalid request.", 400);
            if (!Allowed.Any(a => path.StartsWith(a, StringComparison.OrdinalIgnoreCase))) return Fail("Invalid request.", 400);
            endpoint = $"api/{path}";
            return null;
        }

        [HttpGet("{**path}")]
        public async Task<IActionResult> Get(string path)
        {
            var bad = Guard(path, out var endpoint);
            if (bad != null) return bad;
            var query = Request.Query.Where(q => !string.IsNullOrEmpty(q.Key)).ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            if (!query.ContainsKey("companyId")) query["CompanyId"] = ActiveCompanyId(Session()!).ToString();
            var qs = string.Join("&", query.Select(q => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value)}"));
            return Relay(await _api.GetRawAsync($"{endpoint}?{qs}"));
        }

        [HttpPost("{**path}")]
        public async Task<IActionResult> Post(string path)
        {
            var bad = Guard(path, out var endpoint);
            if (bad != null) return bad;
            return Relay(await _api.PostRawAsync(WithQuery(endpoint), await BodyAsync()));
        }

        [HttpPut("{**path}")]
        public async Task<IActionResult> Put(string path)
        {
            var bad = Guard(path, out var endpoint);
            if (bad != null) return bad;
            return Relay(await _api.PutRawAsync(WithQuery(endpoint), await BodyAsync()));
        }

        [HttpDelete("{**path}")]
        public async Task<IActionResult> Delete(string path)
        {
            var bad = Guard(path, out var endpoint);
            if (bad != null) return bad;
            return Relay(await _api.DeleteRawAsync(WithQuery(endpoint)));
        }

        private string WithQuery(string endpoint) => Request.QueryString.HasValue ? endpoint + Request.QueryString.Value : endpoint;

        private async Task<object> BodyAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var text = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(text)) return new { };
            try
            {
                var node = JsonNode.Parse(text);
                if (node is JsonObject obj && !obj.Any(p => string.Equals(p.Key, "companyId", StringComparison.OrdinalIgnoreCase)))
                    obj["companyId"] = ActiveCompanyId(Session()!);
                return (object?)node ?? new { };
            }
            catch (JsonException) { return new { }; }
        }
    }
}
