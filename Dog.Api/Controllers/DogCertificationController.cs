using Dog.Application.Authorization;
using Dog.Application.Features.DogCertifications.Commands;
using Dog.Application.Features.DogCertifications.DTOs;
using Dog.Application.Features.DogCertifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DogCertificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DogCertificationController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetByDog(int dogId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetCertificationsByDogQuery(dogId), ct));

        [HttpGet("{id:int}")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetDogCertificationByIdQuery(id), ct));

        [HttpPost]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> Create([FromBody] CreateDogCertificationDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateDogCertificationCommand(dto), ct));

        [HttpPut("{id:int}")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDogCertificationDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new UpdateDogCertificationCommand(id, dto), ct));

        [HttpDelete("{id:int}")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteDogCertificationCommand(id), ct));
    }
}
