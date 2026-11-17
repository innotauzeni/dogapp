namespace Dog.Portal.Services
{
    /// <summary>
    /// Best-effort image loader for PDF generation. Handles absolute http(s) URLs AND the relative
    /// "/uploads/..." paths the portal stores when cloud storage isn't configured (these used to be
    /// skipped, which left logos and dog photos blank on certificates).
    /// </summary>
    public static class PdfImageLoader
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task<byte[]?> LoadAsync(string? url, string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var abs) &&
                    (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
                {
                    return await Http.GetByteArrayAsync(abs);
                }

                // Relative path → read straight from this portal's wwwroot (no HTTP round trip).
                var relative = url.TrimStart('~').TrimStart('/', '\\');
                var webRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
                var full = Path.GetFullPath(Path.Combine(webRoot, relative));
                if (full.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
                    return await File.ReadAllBytesAsync(full);

                // Fallback: fetch it from the portal's own public URL.
                if (!string.IsNullOrWhiteSpace(baseUrl) &&
                    Uri.TryCreate(new Uri(baseUrl), "/" + relative, out var rel))
                {
                    return await Http.GetByteArrayAsync(rel);
                }
            }
            catch { /* image is optional */ }
            return null;
        }
    }
}
