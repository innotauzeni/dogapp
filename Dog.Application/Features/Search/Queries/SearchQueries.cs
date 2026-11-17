using Dog.Application.Common;
using Dog.Application.Features.Search.DTOs;
using MediatR;

namespace Dog.Application.Features.Search.Queries
{
    public record GlobalSearchQuery(int CompanyId, string Query) : IRequest<ApiResponse<SearchResultsDto>>;
}
