namespace Dog.Application.Features.Analytics.DTOs
{
    public class MonthlyPointDto
    {
        public string Month { get; set; } = string.Empty; // "2026-07"
        public string Label { get; set; } = string.Empty;  // "Jul 2026"
        public decimal Amount { get; set; }
    }

    public class CategoryBreakdownDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class HandlerActivityStatDto
    {
        public string HandlerName { get; set; } = string.Empty;
        public int ActivityCount { get; set; }
    }

    public class AnalyticsSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }

        public int TotalDogs { get; set; }
        public int ActiveDogs { get; set; }
        public int DeployedDogs { get; set; }
        public int VaccinatedDogs { get; set; }

        public List<MonthlyPointDto> RevenueByMonth { get; set; } = new();
        public List<MonthlyPointDto> ExpensesByMonth { get; set; } = new();
        public List<CategoryBreakdownDto> ExpensesByCategory { get; set; } = new();
        public List<HandlerActivityStatDto> HandlerActivity { get; set; } = new();
    }
}
