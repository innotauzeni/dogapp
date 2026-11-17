using Dog.Application.Common;
using Dog.Application.Features.Deployments.DTOs;
using MediatR;

namespace Dog.Application.Features.Deployments.Commands
{
    public record CreateDeploymentCommand(CreateDeploymentDto Dto) : IRequest<ApiResponse<DeploymentDto>>;
    public record UpdateDeploymentCommand(int Id, CreateDeploymentDto Dto) : IRequest<ApiResponse<DeploymentDto>>;
    public record DeleteDeploymentCommand(int Id) : IRequest<ApiResponse<bool>>;

    /// Recalls a deployed dog back to its branch/head office — marks the deployment
    /// inactive and stamps EndDate. There's nothing else to reset: Dogtbl carries no
    /// location fields of its own, "IsDeployed"/"DeployedLocation" are computed at read
    /// time from whichever Deployment row is currently active for the dog.
    public record EndDeploymentCommand(int Id) : IRequest<ApiResponse<bool>>;
}
