using System.Text.Json.Serialization;

namespace Dog.Application.Features.Countries.DTOs
{
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

    public class CreateCountryDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCountryDto: CreateCountryDto
    {
        public int Id { get; set; }
    }

    public class CreateProvinceDto
    {
        public string Name { get; set; } = string.Empty;
        public int CountryId { get; set; }
    }

    public class CreateCityDto
    {
        public string Name { get; set; } = string.Empty;
        public int ProvinceId { get; set; }
    }

    public class ImportSummaryDto
    {
        public int CountriesImported { get; set; }
        public int ProvincesImported { get; set; }
        public int CitiesImported { get; set; }
        public int Skipped { get; set; }
        public string Message { get; set; } = string.Empty;
    }


  
}
