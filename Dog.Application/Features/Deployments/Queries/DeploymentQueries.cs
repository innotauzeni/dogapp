using Dog.Application.Common;
using Dog.Application.Features.Deployments.DTOs;
using MediatR;

namespace Dog.Application.Features.Deployments.Queries
{
    public record GetDeploymentByCompanyQuery(int CompanyId) : IRequest<ApiResponse<List<DeploymentDto>>>;
}
