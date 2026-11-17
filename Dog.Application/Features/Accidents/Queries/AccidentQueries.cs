using Dog.Application.Common;
using Dog.Application.Features.Accidents.DTOs;
using MediatR;

namespace Dog.Application.Features.Accidents.Queries;

public record GetAccidentByIdQuery(int Id) : IRequest<ApiResponse<AccidentDto>>;
public record GetAccidentsByDogQuery(int DogId) : IRequest<ApiResponse<List<AccidentListDto>>>;
public record GetAccidentsByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<AccidentListDto>>>;
public record GetAllAccidentsQuery(int CompanyId) : IRequest<ApiResponse<List<AccidentListDto>>>;
public record GetAccidentsByTypeQuery(int CompanyId, int AccidentType) : IRequest<ApiResponse<List<AccidentListDto>>>;
public record GetAccidentsBySeverityQuery(int CompanyId, int Severity) : IRequest<ApiResponse<List<AccidentListDto>>>;
public record GetUnresolvedAccidentsQuery(int CompanyId) : IRequest<ApiResponse<List<AccidentListDto>>>;
