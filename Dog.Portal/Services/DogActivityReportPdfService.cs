using Dog.Portal.Helpers;
using Dog.Portal.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Portal.Services
{
    /// <summary>Real PDF "Dog Activity Report" (company logo + details, dog details, care, vaccinations, breeding).
    /// Replaces the old browser print-page so the output is a downloadable PDF, not an HTML page.</summary>
    public static class DogActivityReportPdfService
    {
        private const string Brand = "#667eea";
        private const string Brand2 = "#764ba2";
        private const string Ink = "#1e293b";
        private const string Muted = "#64748b";
        private const string Line = "#e2e8f0";
        private const string Soft = "#f8fafc";

        public static byte[] Build(DogProfileDto profile, CompanyDto? company, byte[]? logo, byte[]? dogPhoto)
        {
            var d = profile.Dog;
            var companyName = company?.CompanyName ?? d.CompanyName ?? "Dog Activity Report";
            var contact = string.Join("  •  ", new[] { company?.PhoneNumber, company?.EmailAddress, company?.Website }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            var care = (profile.CareRecords ?? new()).OrderByDescending(r => r.OccurredOn).ToList();
            var vaccs = (profile.Vaccinations ?? new()).OrderByDescending(v => v.VaccinationDate).ToList();
            var matings = (profile.MatingRecords ?? new()).OrderByDescending(m => m.MatingDate).ToList();
            var a = profile.CurrentAssignment;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(Ink).FontFamily("Lato"));

                    page.Header().Column(h =>
                    {
                        h.Item().Background(Brand).Padding(14).Row(row =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                row.ConstantItem(48).Height(48).Background("#ffffff").Padding(3).Image(logo).FitArea();
                                row.ConstantItem(12);
                            }
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(companyName).FontSize(16).Bold().FontColor("#ffffff");
                                c.Item().Text("Dog Activity Report").FontSize(10).SemiBold().FontColor("#ffffff");
                                if (!string.IsNullOrWhiteSpace(company?.Address))
                                    c.Item().Text(company!.Address!).FontSize(8.5f).FontColor("#e0e7ff");
                                if (!string.IsNullOrWhiteSpace(contact))
                                    c.Item().Text(contact).FontSize(8.5f).FontColor("#e0e7ff");
                            });
                        });
                        h.Item().Height(8);
                    });

                    page.Content().Column(col =>
                    {
                        // Dog summary
                        col.Item().Border(1).BorderColor(Line).Background(Soft).Padding(12).Row(row =>
                        {
                            if (dogPhoto is { Length: > 0 })
                            {
                                row.ConstantItem(86).Height(86).Image(dogPhoto).FitArea();
                                row.ConstantItem(14);
                            }
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(d.Name).FontSize(20).Bold();
                                c.Item().Text($"{(string.IsNullOrWhiteSpace(d.Breed) ? "Unknown breed" : d.Breed)}  •  {d.Color}  •  {d.Gender ?? "Unknown gender"}").FontColor(Muted);
                                c.Item().PaddingTop(6).Row(r =>
                                {
                                    r.RelativeItem().Column(l =>
                                    {
                                        Kv(l, "Date of Birth", d.DateOfBirth.ToString("MMMM dd, yyyy"));
                                        Kv(l, "Microchip", string.IsNullOrWhiteSpace(d.MicrochipNumber) ? "Not registered" : d.MicrochipNumber!);
                                        Kv(l, "Status", d.DateOfDeath.HasValue ? $"Deceased ({d.DateOfDeath:MMM dd, yyyy})" : (d.IsActive ? "Active" : "Inactive"));
                                    });
                                    r.RelativeItem().Column(rt =>
                                    {
                                        Kv(rt, "Sire (Father)", string.IsNullOrWhiteSpace(d.SireName) ? "Unknown" : d.SireName!);
                                        Kv(rt, "Dam (Mother)", string.IsNullOrWhiteSpace(d.DamName) ? "Unknown" : d.DamName!);
                                        Kv(rt, "Handler", a == null || string.IsNullOrWhiteSpace(a.HandlerName) ? "Not assigned" : a.HandlerName!);
                                        Kv(rt, "Branch", a == null || string.IsNullOrWhiteSpace(a.BranchName) ? "Not assigned" : a.BranchName!);
                                    });
                                });
                            });
                        });

                        // Quick stats
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            Stat(row, care.Count.ToString(), "Care records");
                            Stat(row, vaccs.Count.ToString(), "Vaccinations");
                            Stat(row, matings.Count.ToString(), "Matings");
                            Stat(row, care.Count(r => r.RequiresAttention).ToString(), "Needs attention");
                        });

                        // Care timeline
                        Section(col, "Care Timeline", $"{care.Count} record{(care.Count == 1 ? "" : "s")}");
                        if (care.Count == 0)
                            col.Item().PaddingVertical(6).Text("No care records found.").FontColor(Muted);
                        foreach (var r in care)
                        {
                            col.Item().PaddingBottom(6).ShowEntire().Border(1).BorderColor(r.RequiresAttention ? "#fecaca" : Line)
                                .Background(r.RequiresAttention ? "#fff7f7" : Soft).Padding(8).Column(c =>
                            {
                                c.Item().Row(rr =>
                                {
                                    rr.RelativeItem().Text(r.EventType).Bold().FontColor(Brand);
                                    rr.AutoItem().Text(DateDisplay.EventWhen(r.OccurredOn, "MMM dd, yyyy")).FontSize(8.5f).FontColor(Muted);
                                });
                                if (!string.IsNullOrWhiteSpace(r.Details))
                                    c.Item().PaddingTop(3).Text(r.Details!);
                                if (!string.IsNullOrWhiteSpace(r.QuantityOrOutcome))
                                    c.Item().PaddingTop(2).Text(r.QuantityOrOutcome!).FontColor(Brand).SemiBold();
                                var meta = "Recorded by: " + (string.IsNullOrWhiteSpace(r.RecordedByName) ? "System" : r.RecordedByName)
                                           + (r.RequiresAttention ? "   •   NEEDS ATTENTION" : "");
                                c.Item().PaddingTop(3).Text(meta).FontSize(8).FontColor(r.RequiresAttention ? "#dc2626" : Muted);
                            });
                        }

                        // Vaccinations
                        if (vaccs.Count > 0)
                        {
                            Section(col, "Vaccination Records", $"{vaccs.Count} record{(vaccs.Count == 1 ? "" : "s")}");
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                });
                                t.Header(h =>
                                {
                                    foreach (var title in new[] { "Vaccine", "Date", "Next due", "Batch", "Clinic" })
                                        h.Cell().BorderBottom(1).BorderColor(Line).PaddingVertical(3).Text(title).FontSize(8).Bold().FontColor(Muted);
                                });
                                foreach (var v in vaccs)
                                {
                                    var name = string.IsNullOrWhiteSpace(v.Name) ? (v.VaccType ?? "Vaccination") : v.Name!;
                                    Cell(t, name);
                                    Cell(t, v.VaccinationDate.ToString("MMM dd, yyyy"));
                                    Cell(t, v.NextVaccinationDate?.ToString("MMM dd, yyyy") ?? "—");
                                    Cell(t, v.BatchNumber ?? "—");
                                    Cell(t, v.VetclinicName ?? "—");
                                }
                            });
                        }

                        // Matings & litters
                        if (matings.Count > 0)
                        {
                            Section(col, "Mating Records & Litters", $"{matings.Count} record{(matings.Count == 1 ? "" : "s")}");
                            foreach (var m in matings)
                            {
                                col.Item().PaddingBottom(6).ShowEntire().Border(1).BorderColor(Line).Padding(8).Column(c =>
                                {
                                    c.Item().Row(rr =>
                                    {
                                        rr.RelativeItem().Text($"Sire: {m.SireName ?? "Unknown"}   •   Dam: {m.DamName ?? "Unknown"}").SemiBold();
                                        rr.AutoItem().Text(m.MatingDate.ToString("MMM dd, yyyy")).FontSize(8.5f).FontColor(Muted);
                                    });
                                    if (!string.IsNullOrWhiteSpace(m.Location))
                                        c.Item().Text($"Location: {m.Location}").FontSize(8.5f).FontColor(Muted);
                                    if (!string.IsNullOrWhiteSpace(m.Notes))
                                        c.Item().Text(m.Notes!).FontSize(8.5f).Italic().FontColor(Muted);
                                    foreach (var l in m.Litters ?? new())
                                    {
                                        c.Item().PaddingTop(4).Text($"Litter {l.BirthDate:MMM dd, yyyy} — {l.TotalBorn} born, {l.SurvivedCount} survived, {l.DiedCount} died").SemiBold().FontColor(Brand2);
                                        var pups = string.Join(", ", (l.Puppies ?? new()).Select(p => $"{p.Name}{(string.IsNullOrWhiteSpace(p.Gender) ? "" : $" ({p.Gender})")}{(p.DateOfDeath.HasValue ? " †" : "")}"));
                                        if (!string.IsNullOrWhiteSpace(pups))
                                            c.Item().Text("Puppies: " + pups).FontSize(8.5f);
                                    }
                                });
                            }
                        }
                    });

                    page.Footer().BorderTop(1).BorderColor(Line).PaddingTop(4).Row(f =>
                    {
                        f.RelativeItem().Text($"{companyName}  •  Generated {DateTime.Now:MMMM dd, yyyy}  •  Confidential").FontSize(8).FontColor(Muted);
                        f.AutoItem().Text(x =>
                        {
                            x.Span("Page ").FontSize(8).FontColor(Muted);
                            x.CurrentPageNumber().FontSize(8).FontColor(Muted);
                            x.Span(" / ").FontSize(8).FontColor(Muted);
                            x.TotalPages().FontSize(8).FontColor(Muted);
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void Kv(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(3).Text(t =>
            {
                t.Span(label + ": ").FontSize(8.5f).Bold().FontColor(Muted);
                t.Span(value).FontSize(9.5f);
            });
        }

        private static void Stat(RowDescriptor row, string number, string label)
        {
            row.RelativeItem().Border(1).BorderColor(Line).Padding(6).Column(c =>
            {
                c.Item().AlignCenter().Text(number).FontSize(16).Bold().FontColor(Brand);
                c.Item().AlignCenter().Text(label.ToUpperInvariant()).FontSize(7).Bold().FontColor(Muted);
            });
            row.ConstantItem(6);
        }

        private static void Section(ColumnDescriptor col, string title, string count)
        {
            col.Item().PaddingTop(14).PaddingBottom(6).BorderBottom(2).BorderColor(Line).Row(r =>
            {
                r.RelativeItem().Text(title).FontSize(12).Bold();
                r.AutoItem().Text(count).FontSize(8.5f).FontColor(Muted);
            });
        }

        private static void Cell(TableDescriptor t, string text)
            => t.Cell().BorderBottom(1).BorderColor(Line).PaddingVertical(3).Text(text).FontSize(9);
    }
}
