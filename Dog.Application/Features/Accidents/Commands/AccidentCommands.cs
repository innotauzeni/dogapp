using Dog.Application.Common;
using Dog.Application.Features.Accidents.DTOs;
using MediatR;

namespace Dog.Application.Features.Accidents.Commands;

public record CreateAccidentCommand(CreateAccidentDto Dto) : IRequest<ApiResponse<AccidentDto>>;
public record UpdateAccidentCommand(UpdateAccidentDto Dto) : IRequest<ApiResponse<AccidentDto>>;
public record DeleteAccidentCommand(int Id) : IRequest<ApiResponse<bool>>;
public record MarkAccidentResolvedCommand(int Id, string? Resolution) : IRequest<ApiResponse<bool>>;
