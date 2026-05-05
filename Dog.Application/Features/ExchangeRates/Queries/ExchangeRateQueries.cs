using Dog.Application.Common;
using Dog.Application.Features.ExchangeRates.DTOs;
using MediatR;

namespace Dog.Application.Features.ExchangeRates.Queries
{
    public record GetExchangeRatesByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<ExchangeRateDto>>>;

}
