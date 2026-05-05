using Dog.Application.Common;
using Dog.Application.Features.Currencies.DTOs;
using MediatR;

namespace Dog.Application.Features.Currencies.Commands
{
    // Currency
    public record CreateCurrencyCommand(CreateCurrencyDto Dto) : IRequest<ApiResponse<CurrencyDto>>;
    public record UpdateCurrencyCommand(int Id, CreateCurrencyDto Dto) : IRequest<ApiResponse<CurrencyDto>>;
    public record DeleteCurrencyCommand(int Id) : IRequest<ApiResponse<bool>>;
    public record ToggleCurrencyCommand(int Id) : IRequest<ApiResponse<bool>>;
}
