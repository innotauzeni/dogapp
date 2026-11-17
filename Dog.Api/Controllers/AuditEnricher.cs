using Dog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Dog.Api.Controllers
{
    /// <summary>
    /// Turns raw audit rows into something readable: pulls the reserved "__previous" / "__actor"
    /// blocks out of the stored JSON, and resolves the user id to the person's real details.
    /// </summary>
    public static class AuditEnricher
    {
        public static async Task EnrichAsync(ApplicationDbContext db, IReadOnlyCollection<AuditItemDto> items, CancellationToken ct)
        {
            var metaByItem = new Dictionary<AuditItemDto, Dictionary<string, object>?>();

            foreach (var item in items)
            {
                Dictionary<string, object>? meta = null;
                if (item.Changes != null)
                {
                    if (item.Changes.Remove(ApplicationDbContext.PreviousKey, out var prev))
                        item.Previous = ToDictionary(prev);
                    if (item.Changes.Remove(ApplicationDbContext.ActorKey, out var act))
                        meta = ToDictionary(act);
                }
                metaByItem[item] = meta;
            }

            var ids = items.Select(i => i.Username)
                .Where(u => !string.IsNullOrEmpty(u) && u != "System")
                .Distinct().ToList();

            var users = ids.Count == 0
                ? new Dictionary<string, UserInfo>()
                : await db.Users.Where(u => ids.Contains(u.Id))
                    .Select(u => new UserInfo
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Surname = u.Surname,
                        Email = u.Email,
                        Phone = u.PhoneNumber,
                        Role = u.RoleName ?? u.Role,
                        CompanyId = u.CompanyId,
                    })
                    .ToDictionaryAsync(u => u.Id, ct);

            var companyIds = users.Values.Select(u => u.CompanyId).Distinct().ToList();
            var companies = companyIds.Count == 0
                ? new Dictionary<int, string?>()
                : await db.ka_companies.IgnoreQueryFilters().Where(c => companyIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.CompanyName, ct);

            foreach (var item in items)
            {
                metaByItem.TryGetValue(item, out var meta);
                string Meta(string key) => meta != null && meta.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

                var rawId = item.Username;
                var actor = new AuditActorDto
                {
                    UserId = rawId,
                    IpAddress = NullIfEmpty(Meta("ip")),
                    UserAgent = NullIfEmpty(Meta("userAgent")),
                    Method = NullIfEmpty(Meta("method")),
                    Path = NullIfEmpty(Meta("path")),
                    Email = NullIfEmpty(Meta("email")),
                    Role = NullIfEmpty(Meta("role")),
                    FullName = NullIfEmpty(Meta("userName")),
                };

                if (rawId != null && users.TryGetValue(rawId, out var u))
                {
                    var full = ((u.Name ?? "") + " " + (u.Surname ?? "")).Trim();
                    if (full.Length > 0) actor.FullName = full;
                    actor.Email = u.Email ?? actor.Email;
                    actor.Phone = u.Phone;
                    actor.Role = u.Role ?? actor.Role;
                    if (companies.TryGetValue(u.CompanyId, out var cn)) actor.CompanyName = cn;
                }
                else if (rawId == "System")
                {
                    actor.FullName = "System (automatic / background)";
                }

                item.Actor = actor;
                // The list shows a readable name instead of a raw GUID where we can resolve one.
                if (!string.IsNullOrWhiteSpace(actor.FullName)) item.Username = actor.FullName;
            }
        }

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

        // Stored blocks come back from Newtonsoft as JObject (not safe for System.Text.Json), so
        // round-trip them into a plain dictionary of primitives.
        private static Dictionary<string, object> ToDictionary(object? value)
        {
            if (value == null) return new Dictionary<string, object>();
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(value))
                       ?? new Dictionary<string, object>();
            }
            catch { return new Dictionary<string, object>(); }
        }

        private sealed class UserInfo
        {
            public string Id { get; set; } = "";
            public string? Name { get; set; }
            public string? Surname { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Role { get; set; }
            public int CompanyId { get; set; }
        }
    }
}
