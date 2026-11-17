using Dog.Application.Features.Animals.DTOs;
using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.DogCertifications.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Shared, branded training/achievement certificate PDF for a single
    /// DogCertification record (QuestPDF). Used by the web Portal.</summary>
    public static class DogCertificationPdfBuilder
    {
        private const string Brand = "#f59e0b";
        private const string Brand2 = "#d97706";
        private const string Ink = "#1e293b";
        private const string Muted = "#64748b";
        private const string Line = "#e2e8f0";

        public static byte[] Build(DogCertificationDto cert, AnimalListDto dog, CompanyDto? company, byte[]? logo, byte[]? dogPhoto)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink).FontFamily("Lato"));

                    page.Content().Border(2).BorderColor(Brand).Padding(2).Column(outer =>
                    {
                        outer.Item().Border(1).BorderColor(Line).Padding(32).Column(col =>
                        {
                            // Company header
                            col.Item().Row(row =>
                            {
                                if (logo is { Length: > 0 })
                                {
                                    row.ConstantItem(50).Height(50).Image(logo).FitArea();
                                    row.ConstantItem(12);
                                }
                                row.RelativeItem().Column(info =>
                                {
                                    info.Item().Text(company?.CompanyName ?? dog.CompanyName ?? "Dog Section").FontSize(14).Bold().FontColor(Brand2);
                                    var meta = string.Join("  •  ", new[] { company?.PhoneNumber, company?.EmailAddress, company?.Address }.Where(s => !string.IsNullOrWhiteSpace(s)));
                                    if (!string.IsNullOrWhiteSpace(meta))
                                        info.Item().Text(meta).FontSize(8.5f).FontColor(Muted);
                                });
                                row.ConstantItem(150).AlignRight().Text($"No. CERT-{cert.Id:D5}").FontSize(9).SemiBold().FontColor(Muted);
                            });

                            col.Item().PaddingVertical(16).LineHorizontal(1).LineColor(Line);

                            // Title
                            col.Item().AlignCenter().Text("CERTIFICATE OF ACHIEVEMENT").FontSize(24).Bold().FontColor(Brand2).LetterSpacing(0.04f);
                            col.Item().AlignCenter().PaddingTop(4).Text("This certifies that").FontSize(11).FontColor(Muted);

                            // Photo + dog name
                            col.Item().PaddingTop(14).AlignCenter().Row(row =>
                            {
                                if (dogPhoto is { Length: > 0 })
                                {
                                    row.AutoItem().Height(64).Width(64).Image(dogPhoto).FitArea();
                                    row.ConstantItem(14);
                                }
                                row.AutoItem().AlignMiddle().Text(dog.Name).FontSize(28).Bold().FontColor(Ink);
                            });
                            col.Item().AlignCenter().PaddingTop(2).Text($"{dog.Breed ?? "Unknown breed"}  •  {dog.Color ?? ""}").FontSize(11).FontColor(Muted);

                            col.Item().AlignCenter().PaddingTop(18).Text("has successfully achieved").FontSize(11).FontColor(Muted);
                            col.Item().AlignCenter().PaddingTop(6).Text(cert.CertificationName).FontSize(20).Bold().FontColor(Brand2);

                            // Detail grid
                            col.Item().PaddingTop(24).Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    Field(left, "Issuing Body", string.IsNullOrWhiteSpace(cert.IssuingBody) ? "Not specified" : cert.IssuingBody!);
                                    Field(left, "Trainer", string.IsNullOrWhiteSpace(cert.TrainerName) ? "Not specified" : cert.TrainerName!);
                                });
                                row.ConstantItem(24);
                                row.RelativeItem().Column(right =>
                                {
                                    Field(right, "Issue Date", cert.IssueDate.ToString("MMMM dd, yyyy"));
                                    Field(right, "Expiry Date", cert.ExpiryDate.HasValue ? cert.ExpiryDate.Value.ToString("MMMM dd, yyyy") : "Does not expire");
                                });
                            });

                            if (!string.IsNullOrWhiteSpace(cert.Notes))
                            {
                                col.Item().PaddingTop(6).Column(n =>
                                {
                                    n.Item().Text("NOTES").FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.05f);
                                    n.Item().Text(cert.Notes!).FontSize(10).FontColor(Ink);
                                });
                            }

                            col.Item().PaddingTop(28).Row(row =>
                            {
                                row.RelativeItem().Column(sig =>
                                {
                                    sig.Item().PaddingTop(20).Width(180).LineHorizontal(1).LineColor(Ink);
                                    sig.Item().Text("Authorised Signature").FontSize(9).FontColor(Muted);
                                });
                                row.RelativeItem().AlignRight().Column(dt =>
                                {
                                    dt.Item().AlignRight().Text($"Issued: {DateTime.Now:MMMM dd, yyyy}").FontSize(9).FontColor(Muted);
                                    dt.Item().AlignRight().Text(company?.CompanyName ?? dog.CompanyName ?? "").FontSize(9).SemiBold().FontColor(Brand2);
                                });
                            });
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void Field(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(10).Column(c =>
            {
                c.Item().Text(label.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(Muted).LetterSpacing(0.05f);
                c.Item().Text(value).FontSize(11).FontColor(Ink);
            });
        }
    }
}
