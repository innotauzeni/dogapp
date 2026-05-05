using Dog.Application.Features.ExchangeRates.DTOs;

namespace Dog.Portal.Models
{
    public class ExchangeRatesListViewModel
    {
        public List<ExchangeRateDto> ExchangeRateList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    // ── AJAX request models ───────────────────────────────────
    public class CreateExchangeRateRequest
    {
        public int PrimaryCurrencyId { get; set; }
        public int SecondaryCurrencyId { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } = true;
        public int CompanyId { get; set; }
    }

    public class UpdateExchangeRateRequest : CreateExchangeRateRequest
    {
        public int Id { get; set; }
    }
}