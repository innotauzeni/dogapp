using Dog.Application.Features.Companies.DTOs;
using Dog.Application.Features.DogTraining.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Weekly training progress report (staff download / print once approved).</summary>
    public static class ProgressReportPdfBuilder
    {
        private const string Blue = "#1D4ED8";
        private const string Ink = "#0F172A";
        private const string Muted = "#64748B";
        private const string Line = "#CBD5E1";

        public static byte[] Build(ProgressReportDto r, CompanyDto? company, byte[]? logo)
        {
            var org = company?.CompanyName ?? "Dog Training";
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink).FontFamily("Lato"));

                    page.Header().Row(h =>
                    {
                        if (logo is { Length: > 0 }) { h.ConstantItem(48).Height(48).Image(logo).FitArea(); h.ConstantItem(10); }
                        h.RelativeItem().Column(c =>
                        {
                            c.Item().Text(org).FontSize(14).Bold().FontColor(Blue);
                            c.Item().Text("Weekly Training Progress Report").FontSize(17).Bold();
                        });
                        h.ConstantItem(130).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Week {r.WeekNumber}").FontSize(18).Bold().FontColor(Blue);
                            c.Item().AlignRight().Text(r.Status).FontSize(9).FontColor(Muted);
                        });
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Border(0.8f).BorderColor(Line).Padding(8).Row(row =>
                        {
                            Meta(row, "Dog", r.DogName);
                            Meta(row, "Customer", r.CustomerName ?? "—");
                            Meta(row, "Handler", r.HandlerName ?? "—");
                            Meta(row, "Enrolment", r.EnrollmentNumber ?? "—");
                        });

                        col.Item().PaddingTop(6).Row(row =>
                        {
                            Meta(row, "Days logged", r.LogsInWeek.ToString());
                            Meta(row, "Average rating", r.AverageRating.HasValue ? $"{r.AverageRating}/10" : "—");
                            Meta(row, "Submitted", r.SubmittedOn?.ToString("dd MMM yyyy") ?? "—");
                            Meta(row, "Approved by", r.ReviewedByName ?? "—");
                        });

                        Block(col, "Training objectives", r.Objectives);
                        Block(col, "Activities completed", r.ActivitiesCompleted);
                        Block(col, "Activities outstanding", r.ActivitiesOutstanding);
                        Block(col, "Performance summary", r.PerformanceSummary);
                        Block(col, "Challenges encountered", r.Challenges);
                        Block(col, "Improvements", r.Improvements);
                        Block(col, "Handler recommendations", r.Recommendations);
                        if (!string.IsNullOrWhiteSpace(r.ReviewComments)) Block(col, "Manager's comments", r.ReviewComments);
                    });

                    page.Footer().AlignCenter().Text($"{org} • {DateTime.Now:dd MMM yyyy}").FontSize(8).FontColor(Muted);
                });
            });
            return doc.GeneratePdf();
        }

        private static void Meta(RowDescriptor row, string label, string value)
            => row.RelativeItem().Column(c =>
            {
                c.Item().Text(label.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(Muted);
                c.Item().Text(value).SemiBold();
            });

        private static void Block(ColumnDescriptor col, string title, string? text)
        {
            col.Item().PaddingTop(10).Column(c =>
            {
                c.Item().BorderBottom(1).BorderColor(Blue).PaddingBottom(2).Text(title).Bold().FontColor(Blue);
                c.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(text) ? "—" : text);
            });
        }
    }
}
