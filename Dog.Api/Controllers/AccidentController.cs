using Dog.Application.Authorization;
using Dog.Application.Features.Accidents.Commands;
using Dog.Application.Features.Accidents.DTOs;
using Dog.Application.Features.Accidents.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccidentController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccidentController(IMediator mediator) => _mediator = mediator;

    [HttpPost("Create")]
    [HasPermission("accident.create")]
    public async Task<IActionResult> CreateAccident([FromBody] CreateAccidentDto dto, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateAccidentCommand(dto), ct));

    [HttpPut("Update/{id:int}")]
    [HasPermission("accident.edit")]
    public async Task<IActionResult> UpdateAccident(int id, [FromBody] UpdateAccidentDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return Ok(await _mediator.Send(new UpdateAccidentCommand(dto), ct));
    }

    [HttpDelete("Delete/{id:int}")]
    [HasPermission("accident.delete")]
    public async Task<IActionResult> DeleteAccident(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteAccidentCommand(id), ct));

    [HttpPost("MarkResolved/{id:int}")]
    [HasPermission("accident.edit")]
    public async Task<IActionResult> MarkAccidentResolved(int id, [FromBody] string? resolution, CancellationToken ct)
        => Ok(await _mediator.Send(new MarkAccidentResolvedCommand(id, resolution), ct));

    [HttpGet("GetById/{id:int}")]
    [HasPermission("accident.view")]
    public async Task<IActionResult> GetAccidentById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAccidentByIdQuery(id), ct));

    [HttpGet("GetByDog/{dogId:int}")]
    [HasPermission("accident.view")]
    public async Task<IActionResult> GetAccidentsByDog(int dogId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAccidentsByDogQuery(dogId), ct));

    [HttpGet("GetByCompany/{companyId:int}")]
    [HasPermission("accident.view")]
    public async Task<IActionResult> GetAccidentsByCompany(int companyId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAccidentsByCompanyQuery(companyId), ct));

    [HttpGet("GetAll")]
    [HasPermission("accident.view")]
    public async Task<IActionResult> GetAllAccidents(int companyId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAllAccidentsQuery(companyId), ct));

    [HttpGet("GetByType/{companyId:int}/{accidentType:int}")]
    [HasPermission("accident.view")]
    public async Task<IActionResult> GetAccidentsByType(int companyId, int accidentType, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAccidentsByTypeQuery(companyId, accidentType), ct));

    [HttpGet("GetBySeverity/{companyId:int}/{severity:int}")]
    [HasPermission("accident.view")]
    public async Task<IActionResult> GetAccidentsBySeverity(int companyId, int severity, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAccidentsBySeverityQuery(companyId, severity), ct));

    [HttpGet("GetUnresolved/{companyId:int}")]
    [HasPermission("accident.view")]
    public async Task<IActionResult> GetUnresolvedAccidents(int companyId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUnresolvedAccidentsQuery(companyId), ct));
}
