using Dog.Application.Common;
using Dog.Application.Features.Countries.DTOs;
using MediatR;

namespace Dog.Application.Features.Countries.Commands
{
    // Countries
    public record CreateCountryCommand(CreateCountryDto Dto) : IRequest<ApiResponse<CountryDto>>;
    public record UpdateCountryCommand(int Id, CreateCountryDto Dto) : IRequest<ApiResponse<CountryDto>>;
    public record DeleteCountryCommand(int Id) : IRequest<ApiResponse<bool>>;

    // Provinces
    public record CreateProvinceCommand(CreateProvinceDto Dto) : IRequest<ApiResponse<ProvinceDto>>;
    public record UpdateProvinceCommand(int Id, CreateProvinceDto Dto) : IRequest<ApiResponse<ProvinceDto>>;
    public record DeleteProvinceCommand(int Id) : IRequest<ApiResponse<bool>>;

    // Cities
    public record CreateCityCommand(CreateCityDto Dto) : IRequest<ApiResponse<CityDto>>;
    public record UpdateCityCommand(int Id, CreateCityDto Dto) : IRequest<ApiResponse<CityDto>>;
    public record DeleteCityCommand(int Id) : IRequest<ApiResponse<bool>>;
}
