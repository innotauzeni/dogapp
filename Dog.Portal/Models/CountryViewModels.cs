namespace Dog.Portal.Models
{
    // ── Country ──────────────────────────────────────────────
    public class CountryDto
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int ProvinceCount { get; set; }
    }

    public class ProvinceDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
        public int CityCount { get; set; }
    }

    public class CityDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
        public string? CountryName { get; set; }
    }

    public class ImportSummaryDto
    {
        public int CountriesImported { get; set; }
        public int ProvincesImported { get; set; }
        public int CitiesImported { get; set; }
        public int Skipped { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ── AJAX request bodies ──────────────────────────────────
    public class CreateCountryRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCountryRequest : CreateCountryRequest
    {
        public int Id { get; set; }
    }

    // ── View Models ──────────────────────────────────────────
    public class CountriesListViewModel
    {
        public List<CountryDto> Countries { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalProvinces { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }
}
