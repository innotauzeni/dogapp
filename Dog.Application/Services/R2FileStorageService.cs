using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Dog.Application.Common;
using Dog.Application.Contracts;

namespace Dog.Application.Services
{
    /// <summary>
    /// Uploads files to Cloudflare R2 via the S3-compatible API and returns the
    /// object's public URL. Credentials stay server-side (see <see cref="R2Options"/>).
    /// </summary>
    public class R2FileStorageService : IFileStorageService
    {
        private readonly R2Options _options;
        private readonly IAmazonS3? _client;

        public R2FileStorageService(R2Options options)
        {
            _options = options;
            if (_options.IsConfigured)
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = _options.Endpoint,
                    ForcePathStyle = true,
                    // R2 doesn't support the AWS SDK v4 default integrity
                    // checksums; only send them when the operation requires it.
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                };
                _client = new AmazonS3Client(
                    new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey), config);
            }
        }

        public bool IsEnabled => _client != null;

        public async Task<string> UploadBytesAsync(byte[] bytes, string fileName, string contentType, string folder, CancellationToken ct = default)
        {
            using var ms = new MemoryStream(bytes);
            return await UploadAsync(ms, fileName, contentType, folder, ct);
        }

        public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
        {
            if (_client == null)
                throw new InvalidOperationException("R2 storage is not configured. Set the R2_* environment variables.");

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ExtensionFor(contentType);
            var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{ext}";

            var request = new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                InputStream = content,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                DisablePayloadSigning = true, // required for R2 streaming uploads
            };

            await _client.PutObjectAsync(request, ct);

            return $"{_options.PublicUrl.TrimEnd('/')}/{key}";
        }

        private static string ExtensionFor(string contentType) => contentType switch
        {
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/jpeg" => ".jpg",
            _ => ".bin",
        };
    }
}
