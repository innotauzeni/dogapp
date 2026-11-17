using Dog.Application.Features.Companies.DTOs;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Branded K9 handler training certificate (QuestPDF), awarded to a
    /// handler after completing training with a dog. Landscape, with a QR code
    /// (encodes the certificate details for offline verification since there's no
    /// online verification portal), a gold award-ribbon seal, and Trainer + CEO
    /// signature lines.
    ///
    /// The ribbon seal is drawn via an inline SVG (QuestPDF.Fluent.SvgExtensions.Svg) —
    /// QuestPDF's older raw-Canvas/SkiaSharp API was removed from the public surface
    /// as of 2024.3.0 (ICanvas is now internal), so SVG is the only supported way to
    /// draw custom vector shapes. Verified by rendering to PNG before wiring this up.</summary>
    public static class HandlerCertificatePdfBuilder
    {
        private const string Gold = "#D4AF37";
        private const string GoldDark = "#B8860B";
        private const string Ink = "#1e293b";
        private const string Muted = "#64748b";
        private const string Line = "#e2e8f0";

        private const string RibbonSvg = """
            <svg xmlns="http://www.w3.org/2000/svg" width="100" height="90" viewBox="0 0 100 90">
              <polygon points="36,36 46,76 36,64" fill="#D4AF37"/>
              <polygon points="64,36 54,76 64,64" fill="#D4AF37"/>
              <circle cx="50" cy="30" r="22" fill="#D4AF37" stroke="#B8860B" stroke-width="2"/>
              <circle cx="50" cy="30" r="16" fill="none" stroke="#B8860B" stroke-width="1.5"/>
              <polygon points="50,19.5 53.5,26.7 61.4,27.8 55.7,33.3 57.1,41.2 50,37.4 42.9,41.2 44.3,33.3 38.6,27.8 46.5,26.7" fill="#FFFFFF"/>
            </svg>
            """;

        public static byte[] Build(
            string handlerFullName,
            string? dogName,
            string trainerFullName,
            DateTime issueDate,
            string certificateNumber,
            CompanyDto? company,
            byte[]? logo)
        {
            var qrPng = TryBuildQrCode(
                $"HANDLER TRAINING CERTIFICATE\nCert No: {certificateNumber}\nHandler: {handlerFullName}\n" +
                $"Dog: {dogName ?? "-"}\nTrainer: {trainerFullName}\nIssued: {issueDate:yyyy-MM-dd}\n" +
                $"Issued by: {company?.CompanyName ?? "-"}");

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink).FontFamily("Lato"));

                    page.Content().Border(2).BorderColor(Gold).Padding(2).Column(outer =>
                    {
                        outer.Item().Border(1).BorderColor(Line).Padding(30).Column(col =>
                        {
                            // Company header
                            col.Item().Row(row =>
                            {
                                if (logo is { Length: > 0 })
                                {
                                    row.ConstantItem(48).Height(48).Image(logo).FitArea();
                                    row.ConstantItem(12);
                                }
                                row.RelativeItem().Column(info =>
                                {
                                    info.Item().Text(company?.CompanyName ?? "Dog Section").FontSize(13).Bold().FontColor(GoldDark);
                                    var meta = string.Join("  •  ", new[] { company?.PhoneNumber, company?.EmailAddress, company?.Address }.Where(s => !string.IsNullOrWhiteSpace(s)));
                                    if (!string.IsNullOrWhiteSpace(meta))
                                        info.Item().Text(meta).FontSize(8.5f).FontColor(Muted);
                                });
                                row.ConstantItem(170).AlignRight().Text($"No. HTC-{certificateNumber}").FontSize(9).SemiBold().FontColor(Muted);
                            });

                            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Line);

                            // Gold award-ribbon seal
                            col.Item().AlignCenter().Height(76).Width(86).Svg(RibbonSvg);

                            // Title
                            col.Item().AlignCenter().PaddingTop(2).Text("CERTIFICATE OF COMPLETION").FontSize(25).Bold().FontColor(GoldDark).LetterSpacing(0.04f);
                            col.Item().AlignCenter().PaddingTop(1).Text("K9 HANDLER TRAINING").FontSize(12).SemiBold().FontColor(Muted).LetterSpacing(0.1f);

                            col.Item().AlignCenter().PaddingTop(14).Text("This is to certify that").FontSize(10.5f).FontColor(Muted);
                            col.Item().AlignCenter().PaddingTop(4).Text(handlerFullName).FontSize(25).Bold().FontColor(Ink);

                            // Acknowledgement
                            col.Item().AlignCenter().PaddingTop(8).MaxWidth(480).Text(text =>
                            {
                                text.AlignCenter();
                                text.Span("has successfully completed K9 handler training").FontSize(10.5f).FontColor(Muted);
                                if (!string.IsNullOrWhiteSpace(dogName))
                                {
                                    text.Span(" with ").FontSize(10.5f).FontColor(Muted);
                                    text.Span(dogName).FontSize(10.5f).Bold().FontColor(GoldDark);
                                }
                                text.Span(", demonstrating proficiency in canine handling, obedience control and operational deployment procedures in accordance with the issuing organisation's training standards.")
                                    .FontSize(10.5f).FontColor(Muted);
                            });

                            // Detail grid
                            col.Item().PaddingTop(18).Row(row =>
                            {
                                row.RelativeItem().Column(left => Field(left, "Trainer", trainerFullName));
                                row.ConstantItem(24);
                                row.RelativeItem().Column(mid => Field(mid, "Issue Date", issueDate.ToString("MMMM dd, yyyy")));
                                row.ConstantItem(24);
                                row.RelativeItem().Column(right => Field(right, "Certificate No.", certificateNumber));
                            });

                            // Signatures + QR
                            col.Item().PaddingTop(22).Row(row =>
                            {
                                row.RelativeItem().Column(sig =>
                                {
                                    sig.Item().PaddingTop(18).Width(160).LineHorizontal(1).LineColor(Ink);
                                    sig.Item().Text(trainerFullName).FontSize(10).SemiBold().FontColor(Ink);
                                    sig.Item().Text("Trainer").FontSize(8).FontColor(Muted);
                                });

                                row.RelativeItem().AlignCenter().Column(qr =>
                                {
                                    if (qrPng is { Length: > 0 })
                                        qr.Item().AlignCenter().Height(60).Width(60).Image(qrPng).FitArea();
                                    qr.Item().AlignCenter().PaddingTop(2).Text("Scan to verify").FontSize(7.5f).FontColor(Muted);
                                });

                                row.RelativeItem().Column(sig =>
                                {
                                    sig.Item().AlignRight().PaddingTop(18).Width(160).LineHorizontal(1).LineColor(Ink);
                                    sig.Item().AlignRight().Text("Chief Executive Officer").FontSize(10).SemiBold().FontColor(Ink);
                                    sig.Item().AlignRight().Text("CEO").FontSize(8).FontColor(Muted);
                                });
                            });

                            col.Item().PaddingTop(12).AlignCenter().Text($"Issued {issueDate:MMMM dd, yyyy}  •  {company?.CompanyName ?? ""}").FontSize(8).FontColor(Muted);
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void Field(ColumnDescriptor col, string label, string value)
        {
            col.Item().Column(c =>
            {
                c.Item().AlignCenter().Text(label.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.05f);
                c.Item().AlignCenter().Text(value).FontSize(11).FontColor(Ink);
            });
        }

        /// Encodes the certificate's key details as text so it can be verified by
        /// scanning with any phone camera — there's no online verification portal to
        /// link to, so the QR carries the data itself rather than a URL.
        private static byte[]? TryBuildQrCode(string content)
        {
            try
            {
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
                var png = new PngByteQRCode(data);
                return png.GetGraphic(20);
            }
            catch
            {
                // A QR failure should never block certificate generation.
                return null;
            }
        }
    }
}
