namespace Dog.Application.Contracts
{
    /// <summary>Abstraction over object storage (Cloudflare R2).</summary>
    public interface IFileStorageService
    {
        bool IsEnabled { get; }

        /// <summary>
        /// Uploads <paramref name="content"/> under <paramref name="folder"/>
        /// (e.g. "dogs") and returns the public URL of the stored object.
        /// </summary>
        Task<string> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            string folder,
            CancellationToken ct = default);

        /// <summary>Uploads raw bytes; convenience over <see cref="UploadAsync"/>.</summary>
        Task<string> UploadBytesAsync(
            byte[] bytes,
            string fileName,
            string contentType,
            string folder,
            CancellationToken ct = default);
    }
}
