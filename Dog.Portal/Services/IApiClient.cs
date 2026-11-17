namespace Dog.Portal.Services
{
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
        /// Same as GetAsync but never swallows a failed status code (real status + raw body).
        Task<ApiRawResult> GetRawAsync(string endpoint, CancellationToken ct = default);
        Task<T?> PostAsync<T>(string endpoint, object body, CancellationToken ct = default);
        Task<T?> PutAsync<T>(string endpoint, object body, CancellationToken ct = default);
        Task<T?> DeleteAsync<T>(string endpoint, CancellationToken ct = default);
        Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default);

        /// Same as PostAsync but never swallows a failed status code — returns the
        /// real HTTP status + raw body so the caller can tell "no permission" (403)
        /// apart from "server error" (500) instead of getting null for both.
        Task<ApiRawResult> PostRawAsync(string endpoint, object body, CancellationToken ct = default);

        /// Same as PutAsync but never swallows a failed status code.
        Task<ApiRawResult> PutRawAsync(string endpoint, object body, CancellationToken ct = default);

        /// Same as DeleteAsync but never swallows a failed status code.
        Task<ApiRawResult> DeleteRawAsync(string endpoint, CancellationToken ct = default);
    }

    public class ApiRawResult
    {
        public bool IsSuccessStatusCode { get; set; }
        public int StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
