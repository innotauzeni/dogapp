using Dog.Application.Common;
using Dog.Application.Features.Deployments.DTOs;
using MediatR;

namespace Dog.Application.Features.Deployments.Commands
{
    public record CreateDeploymentCommand(CreateDeploymentDto Dto) : IRequest<ApiResponse<DeploymentDto>>;
    public record UpdateDeploymentCommand(int Id, CreateDeploymentDto Dto) : IRequest<ApiResponse<DeploymentDto>>;
    public record DeleteDeploymentCommand(int Id) : IRequest<ApiResponse<bool>>;
}
