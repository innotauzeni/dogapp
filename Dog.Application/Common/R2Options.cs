using Microsoft.Extensions.Configuration;

namespace Dog.Application.Common
{
    /// <summary>
    /// Cloudflare R2 (S3-compatible) storage configuration.
    ///
    /// Non-secret values (bucket, endpoint, public url, region) can live in
    /// appsettings.json under "R2". The secret keys should NOT be committed —
    /// supply them via environment variables on the server:
    ///   R2_ACCESS_KEY_ID, R2_SECRET_ACCESS_KEY
    /// (R2_BUCKET / R2_ENDPOINT / R2_URL / R2_DEFAULT_REGION are also honoured.)
    /// </summary>
    public class R2Options
    {
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        /// <summary>S3 API endpoint, e.g. https://&lt;account&gt;.r2.cloudflarestorage.com</summary>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>Public base URL for objects, e.g. https://pub-xxxx.r2.dev</summary>
        public string PublicUrl { get; set; } = string.Empty;
        public string Region { get; set; } = "auto";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccessKeyId) &&
            !string.IsNullOrWhiteSpace(SecretAccessKey) &&
            !string.IsNullOrWhiteSpace(Bucket) &&
            !string.IsNullOrWhiteSpace(Endpoint);

        /// <summary>
        /// Builds options from the "R2" config section, then overlays the
        /// R2_* environment variables (which take precedence and keep secrets
        /// out of source control).
        /// </summary>
        public static R2Options Load(IConfiguration configuration)
        {
            var options = configuration.GetSection("R2").Get<R2Options>() ?? new R2Options();

            string? Env(string key) => Environment.GetEnvironmentVariable(key);
            options.AccessKeyId = Env("R2_ACCESS_KEY_ID") ?? options.AccessKeyId;
            options.SecretAccessKey = Env("R2_SECRET_ACCESS_KEY") ?? options.SecretAccessKey;
            options.Bucket = Env("R2_BUCKET") ?? options.Bucket;
            options.Endpoint = Env("R2_ENDPOINT") ?? options.Endpoint;
            options.PublicUrl = Env("R2_URL") ?? options.PublicUrl;
            options.Region = Env("R2_DEFAULT_REGION") ?? options.Region;

            return options;
        }
    }
}
