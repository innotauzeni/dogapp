using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Plain data for the registration certificate, so web Portal, API (WhatsApp) and any
    /// other caller render the exact same design from their own DTO types.</summary>
    public class DogCertificateData
    {
        public int DogId { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public string? Gender { get; set; }
        public string? Color { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDeath { get; set; }
        public string? MicrochipNumber { get; set; }
        public string? SireName { get; set; }
        public string? DamName { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyContact { get; set; }
    }

    /// <summary>Registration certificate — same design as the mobile app's "Certificate of Registration":
    /// double blue frame, centred company logo + name, title, photo, name/breed, details table,
    /// pedigree (sire/dam) boxes and an issue-date / signature footer.</summary>
    public static class DogCertificateLayout
    {
        private const string Blue = "#1D4ED8";
        private const string BlueDark = "#1E3A8A";
        private const string Ink = "#0F172A";
        private const string Muted = "#64748B";
        private const string Line = "#CBD5E1";
        private const string LabelBg = "#F8FAFC";
        private const string BoxBg = "#F1F5F9";

        public static byte[] Build(DogCertificateData d, byte[]? logo, byte[]? dogPhoto)
        {
            var company = string.IsNullOrWhiteSpace(d.CompanyName) ? "Dog Section" : d.CompanyName!;
            var regNo = $"BES-{d.CompanyId}-{d.DogId:D5}";
            var rows = new[]
            {
                new[] { "Sex", Or(d.Gender), "Colour", Or(d.Color) },
                new[] { "Date of Birth", d.DateOfBirth.ToString("dd MMM yyyy"), "Age", Age(d.DateOfBirth, d.DateOfDeath) },
                new[] { "Microchip No.", Or(d.MicrochipNumber), "Registration No.", regNo },
            };

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink).FontFamily("Lato"));

                    page.Content().Border(3).BorderColor(Blue).Padding(6).Border(1).BorderColor(Line)
                        .PaddingHorizontal(28).PaddingVertical(26).Column(col =>
                    {
                        col.Spacing(0);

                        // Header: logo + company
                        col.Item().AlignCenter().Column(h =>
                        {
                            if (logo is { Length: > 0 })
                                h.Item().AlignCenter().Width(60).Height(60).Image(logo).FitArea();
                            h.Item().PaddingTop(8).AlignCenter().Text(company.ToUpperInvariant())
                                .FontSize(16).Bold().FontColor(BlueDark).LetterSpacing(0.06f);
                            if (!string.IsNullOrWhiteSpace(d.CompanyContact))
                                h.Item().AlignCenter().Text(d.CompanyContact!).FontSize(9).FontColor(Muted);
                        });

                        // Title
                        col.Item().PaddingTop(18).AlignCenter().Text("CERTIFICATE OF REGISTRATION")
                            .FontSize(22).Bold().FontColor(BlueDark).LetterSpacing(0.08f);
                        col.Item().PaddingTop(4).AlignCenter().Width(120).Height(2).Background(Blue);
                        col.Item().PaddingTop(16).AlignCenter().Text(
                                $"This is to certify that the dog described below is duly registered in the records of {company}.")
                            .FontSize(11).FontColor(Muted).AlignCenter();

                        // Photo
                        if (dogPhoto is { Length: > 0 })
                        {
                            col.Item().PaddingTop(18).AlignCenter().Width(110).Height(110)
                                .Border(2).BorderColor(Blue).Padding(2).Image(dogPhoto).FitArea();
                        }

                        // Name + breed
                        col.Item().PaddingTop(12).AlignCenter().Text(d.Name).FontSize(30).Bold();
                        col.Item().PaddingTop(2).AlignCenter()
                            .Text(string.IsNullOrWhiteSpace(d.Breed) ? "Unknown breed" : d.Breed!)
                            .FontSize(13).Bold().FontColor(Blue);

                        // Details table
                        col.Item().PaddingTop(20).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(22);
                                c.RelativeColumn(28);
                                c.RelativeColumn(22);
                                c.RelativeColumn(28);
                            });
                            foreach (var r in rows)
                            {
                                Label(t, r[0]); Value(t, r[1]); Label(t, r[2]); Value(t, r[3]);
                            }
                        });

                        // Pedigree
                        col.Item().PaddingTop(16).Text("PEDIGREE").FontSize(10).FontColor(Muted).LetterSpacing(0.15f);
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            Parent(r, "SIRE (Father)", d.SireName);
                            r.ConstantItem(8);
                            Parent(r, "DAM (Mother)", d.DamName);
                        });

                        // Footer pushed to the bottom
                        col.Item().ExtendVertical().AlignBottom().PaddingTop(30).Row(f =>
                        {
                            f.RelativeItem().AlignLeft().Column(x =>
                            {
                                x.Item().Text("Issued on").FontSize(9).FontColor(Muted);
                                x.Item().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(12).Bold();
                            });
                            f.RelativeItem().AlignRight().Column(x =>
                            {
                                x.Item().AlignCenter().Width(160).Height(1).Background(Ink);
                                x.Item().PaddingTop(4).AlignCenter().Text("Authorised Signature").FontSize(10).FontColor(Muted);
                            });
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void Label(TableDescriptor t, string text)
            => t.Cell().Border(0.8f).BorderColor(Line).Background(LabelBg).PaddingHorizontal(8).PaddingVertical(7)
                .Text(text).FontSize(10).FontColor(Muted);

        private static void Value(TableDescriptor t, string text)
            => t.Cell().Border(0.8f).BorderColor(Line).PaddingHorizontal(8).PaddingVertical(7)
                .Text(text).FontSize(11).Bold();

        private static void Parent(RowDescriptor row, string role, string? name)
        {
            row.RelativeItem().Background(BoxBg).Padding(10).Column(c =>
            {
                c.Item().Text(role).FontSize(9).FontColor(Muted).LetterSpacing(0.08f);
                c.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(name) ? "Not recorded" : name!).FontSize(12).Bold();
            });
        }

        private static string Or(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s!;

        /// <summary>Same wording as the mobile app (e.g. "1 yr 3 mo", "4 mo", "9 days").</summary>
        private static string Age(DateTime dob, DateTime? until)
        {
            var end = until ?? DateTime.Now;
            if (end < dob) return "—";
            var years = end.Year - dob.Year;
            var months = end.Month - dob.Month;
            if (end.Day - dob.Day < 0) months--;
            if (months < 0) { years--; months += 12; }
            if (years > 0) return months > 0 ? $"{years} yr {months} mo" : $"{years} yr";
            if (months > 0) return $"{months} mo";
            var days = (int)(end.Date - dob.Date).TotalDays;
            return $"{days} d";
        }
    }
}
