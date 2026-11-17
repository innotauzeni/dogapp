using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.DogTraining.DTOs;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Application.Common.Pdf
{
    /// <summary>"Certificate of Completion" for a finished dog-training programme. Landscape A4 with the organisation
    /// logo/name, a unique certificate number and a QR code that opens the public verification page.</summary>
    public static class TrainingCertificatePdfBuilder
    {
        private const string Navy = "#1E3A8A";
        private const string Blue = "#1D4ED8";
        private const string Gold = "#B8860B";
        private const string Ink = "#0F172A";
        private const string Muted = "#64748B";
        private const string Line = "#CBD5E1";

        public static byte[] Build(TrainingCertificateDto cert, CompanyDto? company, byte[]? logo, string verifyUrl)
        {
            var org = company?.CompanyName ?? "Dog Training";
            var qr = TryBuildQr(verifyUrl);
            var owner = string.IsNullOrWhiteSpace(cert.OwnerName) ? null : cert.OwnerName;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink).FontFamily("Lato"));

                    page.Content().Border(3).BorderColor(Gold).Padding(4).Border(1).BorderColor(Line).Padding(26).Column(col =>
                    {
                        // Header
                        col.Item().AlignCenter().Column(h =>
                        {
                            if (logo is { Length: > 0 })
                                h.Item().AlignCenter().Height(54).Width(54).Image(logo).FitArea();
                            h.Item().PaddingTop(6).AlignCenter().Text(org.ToUpperInvariant()).FontSize(15).Bold().FontColor(Navy).LetterSpacing(0.08f);
                            var meta = string.Join("  •  ", new[] { company?.PhoneNumber, company?.EmailAddress, company?.Website }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (meta.Length > 0) h.Item().AlignCenter().Text(meta).FontSize(8.5f).FontColor(Muted);
                        });

                        col.Item().PaddingTop(14).AlignCenter().Text("CERTIFICATE OF COMPLETION").FontSize(28).Bold().FontColor(Navy).LetterSpacing(0.1f);
                        col.Item().PaddingTop(4).AlignCenter().Width(160).Height(2).Background(Gold);
                        col.Item().PaddingTop(12).AlignCenter().Text("This is to certify that").FontSize(12).FontColor(Muted);

                        col.Item().PaddingTop(4).AlignCenter().Text(cert.DogName).FontSize(34).Bold().FontColor(Ink);
                        col.Item().AlignCenter().Text(string.IsNullOrWhiteSpace(cert.Breed) ? "" : cert.Breed!).FontSize(13).Bold().FontColor(Blue);
                        col.Item().PaddingTop(4).AlignCenter().Text(owner != null ? $"owned by {owner}" : " ").FontSize(12).FontColor(Muted);
                        col.Item().PaddingTop(6).AlignCenter()
                            .Text("has successfully completed the dog training programme shown below.").FontSize(12.5f);

                        // Details
                        col.Item().PaddingTop(14).Row(row =>
                        {
                            Cell(row, "Programme", cert.ProgrammeName);
                            Cell(row, "Duration", $"{cert.DurationDays} days");
                            Cell(row, "Start date", cert.StartDate?.ToString("dd MMM yyyy") ?? "—");
                            Cell(row, "Completion date", cert.CompletionDate?.ToString("dd MMM yyyy") ?? "—");
                            Cell(row, "Handler", string.IsNullOrWhiteSpace(cert.HandlerName) ? "—" : cert.HandlerName!);
                        });

                        // Footer: certificate no., signature, QR
                        col.Item().ExtendVertical().AlignBottom().PaddingTop(14).Row(f =>
                        {
                            f.RelativeItem().AlignLeft().Column(x =>
                            {
                                x.Item().Text("Certificate No.").FontSize(8.5f).FontColor(Muted);
                                x.Item().Text(cert.CertificateNumber).FontSize(13).Bold().FontColor(Navy);
                                x.Item().PaddingTop(4).Text("Issued").FontSize(8.5f).FontColor(Muted);
                                x.Item().Text(cert.IssuedOn.ToString("dd MMM yyyy")).FontSize(11).SemiBold();
                            });

                            f.RelativeItem().AlignCenter().AlignBottom().Column(x =>
                            {
                                x.Item().AlignCenter().Width(190).Height(1).Background(Ink);
                                x.Item().PaddingTop(3).AlignCenter().Text("Authorised Signature").FontSize(9.5f).FontColor(Muted);
                            });

                            f.RelativeItem().AlignRight().Column(x =>
                            {
                                if (qr is { Length: > 0 })
                                    x.Item().AlignRight().Height(70).Width(70).Image(qr).FitArea();
                                x.Item().AlignRight().Text("Scan to verify").FontSize(8).FontColor(Muted);
                            });
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void Cell(RowDescriptor row, string label, string value)
        {
            row.RelativeItem().Border(0.8f).BorderColor(Line).Padding(7).Column(c =>
            {
                c.Item().AlignCenter().Text(label.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.06f);
                c.Item().PaddingTop(2).AlignCenter().Text(value).FontSize(10.5f).Bold();
            });
        }

        private static byte[]? TryBuildQr(string content)
        {
            try
            {
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
                return new PngByteQRCode(data).GetGraphic(20);
            }
            catch { return null; }   // a QR failure must never block the certificate
        }
    }
}
