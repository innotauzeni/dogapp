using Dog.Application.Common;
using Dog.Application.Features.Countries.DTOs;
using MediatR;

namespace Dog.Application.Features.Countries.Queries
{
    /// <summary>Seeds database with a predefined list of countries/provinces/cities (no internet required).</summary>
    public record ImportCountriesLocalQuery : IRequest<ApiResponse<ImportSummaryDto>>;
}
