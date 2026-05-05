namespace Dog.Application.Features.ExchangeRates.DTOs
{

    public class ExchangeRateDto
    {
        public int Id { get; set; }
        public int PrimaryCurrencyId { get; set; }
        public string? PrimaryCurrencyCode { get; set; }
        public int SecondaryCurrencyId { get; set; }
        public string? SecondaryCurrencyCode { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } =true;
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExchangeRateDto
    {
        public int PrimaryCurrencyId { get; set; }
        public int SecondaryCurrencyId { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } = true;
        public int CompanyId { get; set; }
    }

    public class UpdateExchangeRateDto : CreateExchangeRateDto
    {
        public int Id { get; set; }
    }

}
