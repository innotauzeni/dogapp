using Dog.Application.Authorization;
using Dog.Application.Features.Deployments.Commands;
using Dog.Application.Features.Deployments.DTOs;
using Dog.Application.Features.Deployments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeploymentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DeploymentController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("deployment.view")]
        public async Task<IActionResult> GetDeployments(int CompanyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetDeploymentByCompanyQuery(CompanyId), ct));




        [HttpPost("CreateDeployment")]
        [HasPermission("deployment.create")]
        public async Task<IActionResult> CreateDeployment([FromBody] CreateDeploymentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateDeploymentCommand(dto), ct));


        [HttpPut("Deployment/{id:int}")]
        [HasPermission("deployment.edit")]
        public async Task<IActionResult> UpdateDeployment(int id, [FromBody] CreateDeploymentDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateDeploymentCommand(id, dto), ct));
        }

        [HttpDelete("Deployment/{id:int}")]
        [HasPermission("deployment.delete")]
        public async Task<IActionResult> DeleteDeployment(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteDeploymentCommand(id), ct));


       
    }

}
