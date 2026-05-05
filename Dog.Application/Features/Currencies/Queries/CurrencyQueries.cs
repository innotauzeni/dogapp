using Dog.Application.Common;
using Dog.Application.Features.Currencies.DTOs;
using MediatR;

namespace Dog.Application.Features.Currencies.Queries
{
    public record GetAllCurrenciesQuery : IRequest<ApiResponse<List<CurrencyDto>>>;
}
