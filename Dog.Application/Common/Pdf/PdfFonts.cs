using QuestPDF.Drawing;

namespace Dog.Application.Common.Pdf
{
    /// <summary>
    /// Registers the Lato font files that ship with QuestPDF. A "dotnet publish" can put them in the app folder
    /// root instead of LatoFont/, and then QuestPDF finds no font on Linux and prints PDFs with every word missing.
    /// Registering them explicitly from wherever they were published makes PDFs identical on every server.
    /// </summary>
    public static class PdfFonts
    {
        private static bool _done;

        /// <returns>How many font files were registered (0 = none found: copy Lato-*.ttf next to the application).</returns>
        public static int Register()
        {
            if (_done) return -1;
            _done = true;
            var n = 0;
            foreach (var dir in new[] { AppContext.BaseDirectory, Path.Combine(AppContext.BaseDirectory, "LatoFont"), Path.Combine(AppContext.BaseDirectory, "Fonts") })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "Lato-*.ttf"))
                {
                    try { using var s = File.OpenRead(f); FontManager.RegisterFont(s); n++; } catch { /* a bad file must not stop the app */ }
                }
                if (n > 0) break;
            }
            return n;
        }
    }
}
