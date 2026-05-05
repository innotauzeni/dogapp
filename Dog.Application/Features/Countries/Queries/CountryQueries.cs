using Dog.Application.Common;
using Dog.Application.Features.Countries.DTOs;
using MediatR;

namespace Dog.Application.Features.Countries.Queries
{
    public record GetAllCountriesQuery : IRequest<ApiResponse<List<CountryDto>>>;
    public record GetProvincesByCountryQuery(int CountryId) : IRequest<ApiResponse<List<ProvinceDto>>>;
    public record GetCitiesByProvinceQuery(int ProvinceId) : IRequest<ApiResponse<List<CityDto>>>;


    /// <summary>Fetches countries (with provinces/states) from the public REST Countries API and seeds the DB.</summary>
    public record ImportCountriesFromWebQuery : IRequest<ApiResponse<ImportSummaryDto>>;

    /// <summary>Fetches cities for a given country ISO2 code from CountriesNow API and seeds the DB.</summary>
    public record ImportCitiesByCountryQuery(string Iso2) : IRequest<ApiResponse<ImportSummaryDto>>;

    /// <summary>Imports cities for ALL countries already in the DB (can be slow — run once).</summary>
    public record ImportAllCitiesQuery : IRequest<ApiResponse<ImportSummaryDto>>;
}
