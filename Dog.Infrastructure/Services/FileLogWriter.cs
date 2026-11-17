namespace Dog.Infrastructure.Services
{
    /// <summary>
    /// Plain append-only text file logger, sitting alongside the DB-backed
    /// SystemErrorLog table so errors can be checked with just a text editor —
    /// no database tooling needed. Written to log.txt next to the deployed DLLs.
    /// </summary>
    internal static class FileLogWriter
    {
        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private const long MaxSizeBytes = 10 * 1024 * 1024; // rotate past 10MB so the file never grows unbounded

        public static async Task AppendAsync(string entry)
        {
            await Gate.WaitAsync();
            try
            {
                RotateIfTooLarge();
                var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {entry}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
                await File.AppendAllTextAsync(LogPath, line);
            }
            catch
            {
                // Logging must never crash the app it's trying to observe.
            }
            finally
            {
                Gate.Release();
            }
        }

        private static void RotateIfTooLarge()
        {
            try
            {
                var info = new FileInfo(LogPath);
                if (info.Exists && info.Length > MaxSizeBytes)
                {
                    var archivePath = Path.Combine(AppContext.BaseDirectory, $"log.{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
                    File.Move(LogPath, archivePath, overwrite: true);
                }
            }
            catch
            {
                // Best-effort rotation only — a failure here shouldn't block logging.
            }
        }
    }
}
