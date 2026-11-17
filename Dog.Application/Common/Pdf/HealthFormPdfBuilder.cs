using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.DogTraining.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Printable "Health Check Form – Dog" filled in from a health assessment (same sections as the paper form).</summary>
    public static class HealthFormPdfBuilder
    {
        private const string Blue = "#1D4ED8";
        private const string Ink = "#0F172A";
        private const string Muted = "#64748B";
        private const string Line = "#CBD5E1";
        private const string Head = "#EEF2FF";

        public static byte[] Build(HealthAssessmentDto a, CompanyDto? company, byte[]? logo)
        {
            var org = company?.CompanyName ?? "Dog Section";
            var exam = a.Items.Where(i => i.Category == "Exam").OrderBy(i => i.SortOrder).ToList();
            var treat = a.Items.Where(i => i.Category == "Treatment").OrderBy(i => i.SortOrder).ToList();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(26);
                    page.DefaultTextStyle(t => t.FontSize(9).FontColor(Ink).FontFamily("Lato"));

                    page.Header().Row(r =>
                    {
                        if (logo is { Length: > 0 }) { r.ConstantItem(44).Height(44).Image(logo).FitArea(); r.ConstantItem(10); }
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(org).FontSize(13).Bold().FontColor(Blue);
                            c.Item().Text("Health Check Form – Dog").FontSize(17).Bold();
                        });
                        r.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Enrolment {a.EnrollmentNumber}").FontSize(8.5f).FontColor(Muted);
                            c.Item().AlignRight().Text($"Result: {Label(a.Result)}").FontSize(10).Bold();
                        });
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        Section(col, "PET INFORMATION");
                        Grid(col, new[]
                        {
                            ("Name of Pet Customer", a.CustomerName), ("Address", a.CustomerAddress),
                            ("Name of Pet", a.DogName), ("Species", a.Species), ("Breed", a.DogBreed),
                            ("Sex", a.DogGender), ("Weight", a.WeightKg.HasValue ? $"{a.WeightKg:0.##} kg" : null),
                            ("Date of Birth", a.DogDateOfBirth?.ToString("dd MMM yyyy")), ("Date of Exam", a.AssessmentDate.ToString("dd MMM yyyy")),
                        }, 3);

                        Section(col, "INSPECTOR INFORMATION");
                        Grid(col, new[]
                        {
                            ("Name of Inspector", a.ExaminerName), ("Force number #", a.ForceNumber),
                            ("Phone #", a.ExaminerPhone), ("Dog handler", a.DogHandlerName),
                        }, 2);

                        Section(col, "EXAMINATION");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.ConstantColumn(54); c.ConstantColumn(60); c.RelativeColumn(5); });
                            t.Header(h => { Th(h, "Item"); Th(h, "Normal"); Th(h, "Abnormal"); Th(h, "Notes"); });
                            foreach (var i in exam)
                            {
                                Td(t, i.Name, true);
                                Box(t, i.Status == "Normal");
                                Box(t, i.Status == "Abnormal");
                                Td(t, i.Notes ?? "");
                            }
                        });

                        Section(col, "TREATMENT FOR PARASITES & CARE RECORD");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.ConstantColumn(60); c.ConstantColumn(70); c.RelativeColumn(4); c.ConstantColumn(60); });
                            t.Header(h => { Th(h, "Treatment"); Th(h, "Administered"); Th(h, "Date"); Th(h, "Reason"); Th(h, "Notified"); });
                            foreach (var i in treat)
                            {
                                Td(t, i.Name, true);
                                Box(t, i.Administered);
                                Td(t, i.TreatmentDate?.ToString("yyyy/MM/dd") ?? "");
                                Td(t, i.Reason ?? "");
                                Box(t, i.BesNotified);
                            }
                        });
                        col.Item().PaddingTop(4).Text($"Flea/Tick prevention treatment administered on: {(a.FleaTickTreatmentDate?.ToString("dd MMM yyyy") ?? "—")}");

                        Section(col, "OTHER FINDINGS");
                        Grid(col, new[]
                        {
                            ("Physical condition", a.PhysicalCondition), ("Medical conditions", a.MedicalConditions),
                            ("Vaccination status", a.VaccinationStatus), ("Deworming status", a.DewormingStatus),
                        }, 2);

                        Section(col, "CERTIFICATION");
                        Statement(col, a.CertGoodHealth, "On the above date, I completed a health examination on the animal listed above. At that time, I certify that the animal in question was in good health.");
                        Statement(col, a.CertVaccinationsCurrent, "On the above date, I certify that the animal is current on the recommended vaccinations.");
                        Statement(col, a.CertNoParasites, "On the above date, I certify that there was no evidence of endoparasites (roundworms, hookworms, whipworms) or ectoparasites (fleas, mites, ticks, lice) found in or on the animal.");
                        Statement(col, a.CertNoBiteHistory, "I am not aware of any bite history or any conditions, physical or behavioural, which would indicate this animal is unsuitable for public socialisation.");
                        Statement(col, a.Recommendation == "Warning", "I recommend a warning about dog affairs to the customer (last warning).");
                        Statement(col, a.Recommendation == "PositiveChange", "I recommend positive change on the status of the dog from previous visit.");
                        Statement(col, a.Recommendation == "Withdrawal", "I recommend withdrawal of our dog from the client.");

                        col.Item().PaddingTop(6).Text("Any comments:").Bold();
                        col.Item().MinHeight(34).Border(0.8f).BorderColor(Line).Padding(5).Text(a.Comments ?? "");

                        col.Item().PaddingTop(12).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Inspector's signature:").FontSize(8.5f).FontColor(Muted);
                                c.Item().PaddingTop(10).Width(180).LineHorizontal(0.8f).LineColor(Ink);
                                c.Item().Text(a.InspectorSignatureName ?? a.ExaminerName ?? "").SemiBold();
                            });
                            r.ConstantItem(150).Column(c =>
                            {
                                c.Item().Text("Date:").FontSize(8.5f).FontColor(Muted);
                                c.Item().PaddingTop(10).Width(120).LineHorizontal(0.8f).LineColor(Ink);
                                c.Item().Text((a.SignedOn ?? a.AssessmentDate).ToString("dd MMM yyyy")).SemiBold();
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span($"{org} • Printed {DateTime.Now:dd MMM yyyy}  •  Page ").FontSize(8).FontColor(Muted);
                        t.CurrentPageNumber().FontSize(8).FontColor(Muted);
                        t.Span(" / ").FontSize(8).FontColor(Muted);
                        t.TotalPages().FontSize(8).FontColor(Muted);
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static string Label(string r) => r switch { "RequiresFurtherExamination" => "Requires further examination", _ => r };

        private static void Section(ColumnDescriptor col, string title)
            => col.Item().PaddingTop(10).PaddingBottom(3).BorderBottom(1.5f).BorderColor(Blue).Text(title).Bold().FontSize(10).FontColor(Blue);

        private static void Grid(ColumnDescriptor col, (string Label, string? Value)[] cells, int columns)
        {
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c => { for (var i = 0; i < columns; i++) c.RelativeColumn(); });
                foreach (var (label, value) in cells)
                    t.Cell().Border(0.6f).BorderColor(Line).Padding(4).Column(c =>
                    {
                        c.Item().Text(label).FontSize(7.5f).FontColor(Muted);
                        c.Item().Text(string.IsNullOrWhiteSpace(value) ? "—" : value!).SemiBold();
                    });
            });
        }

        private static void Th(TableCellDescriptor h, string text)
            => h.Cell().Background(Head).Border(0.6f).BorderColor(Line).Padding(3).Text(text).FontSize(8).Bold();

        private static void Td(TableDescriptor t, string text, bool bold = false)
        {
            var txt = t.Cell().Border(0.6f).BorderColor(Line).Padding(3).Text(text).FontSize(8.5f);
            if (bold) txt.SemiBold();
        }

        private static void Box(TableDescriptor t, bool on)
            => t.Cell().Border(0.6f).BorderColor(Line).Padding(3).AlignCenter().Text(on ? "X" : "").Bold();

        private static void Statement(ColumnDescriptor col, bool on, string text)
            => col.Item().PaddingTop(3).Row(r =>
            {
                r.ConstantItem(13).Height(11).Border(0.8f).BorderColor(Ink).AlignCenter().AlignMiddle().Text(on ? "X" : "").FontSize(8).Bold();
                r.ConstantItem(6);
                r.RelativeItem().Text(text).FontSize(8.5f);
            });
    }
}
