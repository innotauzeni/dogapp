using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Dog.Portal.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
        {
            _http = http;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task AttachTokenAsync()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var response = await _http.GetAsync(endpoint, ct);
            return await ReadAsync<T>(response);
        }

        public async Task<T?> PostAsync<T>(string endpoint, object body, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(endpoint, content, ct);
            return await ReadAsync<T>(response);
        }

        public async Task<T?> PutAsync<T>(string endpoint, object body, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(endpoint, content, ct);
            return await ReadAsync<T>(response);
        }

        public async Task<T?> DeleteAsync<T>(string endpoint, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var response = await _http.DeleteAsync(endpoint, ct);
            return await ReadAsync<T>(response);
        }

        public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var response = await _http.PostAsync(endpoint, content, ct);
            return await ReadAsync<T>(response);
        }

        public async Task<ApiRawResult> GetRawAsync(string endpoint, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var response = await _http.GetAsync(endpoint, ct);
            return new ApiRawResult
            {
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Body = await response.Content.ReadAsStringAsync(ct),
            };
        }

        public async Task<ApiRawResult> PostRawAsync(string endpoint, object body, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(endpoint, content, ct);
            return new ApiRawResult
            {
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Body = await response.Content.ReadAsStringAsync(ct),
            };
        }

        public async Task<ApiRawResult> PutRawAsync(string endpoint, object body, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(endpoint, content, ct);
            return new ApiRawResult
            {
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Body = await response.Content.ReadAsStringAsync(ct),
            };
        }

        public async Task<ApiRawResult> DeleteRawAsync(string endpoint, CancellationToken ct = default)
        {
            await AttachTokenAsync();
            var response = await _http.DeleteAsync(endpoint, ct);
            return new ApiRawResult
            {
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Body = await response.Content.ReadAsStringAsync(ct),
            };
        }

        private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return default;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
    }

}
