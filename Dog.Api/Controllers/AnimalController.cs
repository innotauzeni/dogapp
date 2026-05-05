using Dog.Application.Authorization;
using Dog.Application.Features.Animals.Commands;
using Dog.Application.Features.Animals.DTOs;
using Dog.Application.Features.Animals.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnimalController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AnimalController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetPets")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetPets(int CompanyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetAnimalsByCompanyQuery(CompanyId), ct));


        [HttpPost("CreatePet")]
        [HasPermission("pet.create")]
        public async Task<IActionResult> CreatePet([FromBody] CreateAnimalDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateAnimalCommand(dto), ct));


        [HttpPut("UpdatePet/{id:int}")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> UpdatePet(int id, [FromBody] UpdateAnimalDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateAnimalCommand(dto), ct));
        }

        [HttpDelete("DeletePet/{id:int}")]
        [HasPermission("pet.delete")]
        public async Task<IActionResult> DeletePet(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteAnimalCommand(id), ct));

        [HttpPost("ToggleStatus/{id:int}")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleAnimalCommand(id), ct));

        [HttpGet("GetDogProfile/{id:int}")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetDogProfile(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetDogProfileQuery(id), ct));

        [HttpPost("AddVaccination")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddVaccination([FromBody] AddVaccinationDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddVaccinationCommand(dto), ct));

        [HttpPost("AddMatingRecord")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddMatingRecord([FromBody] AddMatingRecordDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddMatingRecordCommand(dto), ct));

        [HttpPost("AddLitter")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddLitter([FromBody] AddLitterDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddLitterCommand(dto), ct));

        [HttpGet("GetMatingRecords")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetMatingRecords(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetMatingRecordsByCompanyQuery(CompanyId), ct));

        [HttpPost("AddPuppy")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddPuppy([FromBody] AddPuppyDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddPuppyCommand(dto), ct));

        [HttpGet("GetLitters")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetLitters(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetLittersByCompanyQuery(CompanyId), ct));

        // Reports endpoints
        [HttpGet]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetAnimals([FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? gender = null, CancellationToken ct = default)
        {
            var query = new GetAnimalsQuery 
            { 
                Page = page, 
                PageSize = pageSize, 
                Search = search,
                Status = status,
                Gender = gender
            };
            return Ok(await _mediator.Send(query, ct));
        }

        [HttpGet("vaccinated-count")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetVaccinatedDogsCount(CancellationToken ct)
            => Ok(await _mediator.Send(new GetVaccinatedDogsCountQuery(), ct));

        [HttpGet("vaccinated")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetVaccinatedDogs(CancellationToken ct)
            => Ok(await _mediator.Send(new GetVaccinatedDogsQuery(), ct));

        [HttpGet("litters-count")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetLittersCount(CancellationToken ct)
            => Ok(await _mediator.Send(new GetLittersCountQuery(), ct));

        [HttpGet("litters")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetLittersForReport(CancellationToken ct)
            => Ok(await _mediator.Send(new GetLittersForReportQuery(), ct));

        [HttpGet("mating-count")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetMatingRecordsCount(CancellationToken ct)
            => Ok(await _mediator.Send(new GetMatingRecordsCountQuery(), ct));

        [HttpGet("mating-records")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetMatingRecordsForReport(CancellationToken ct)
            => Ok(await _mediator.Send(new GetMatingRecordsForReportQuery(), ct));

    }








}
