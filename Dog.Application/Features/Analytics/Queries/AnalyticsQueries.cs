using Dog.Application.Common;
using Dog.Application.Features.Analytics.DTOs;
using MediatR;

namespace Dog.Application.Features.Analytics.Queries
{
    public record GetAnalyticsSummaryQuery(int CompanyId, int Months = 6) : IRequest<ApiResponse<AnalyticsSummaryDto>>;
}
