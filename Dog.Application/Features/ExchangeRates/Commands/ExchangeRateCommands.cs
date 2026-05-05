using Dog.Application.Common;
using Dog.Application.Features.ExchangeRates.DTOs;
using MediatR;

namespace Dog.Application.Features.ExchangeRates.Commands
{
    // Exchange Rate
    public record CreateExchangeRateCommand(CreateExchangeRateDto Dto) : IRequest<ApiResponse<ExchangeRateDto>>;
    public record UpdateExchangeRateCommand(UpdateExchangeRateDto Dto) : IRequest<ApiResponse<ExchangeRateDto>>;
    public record DeleteExchangeRateCommand(int Id) : IRequest<ApiResponse<bool>>;

}
