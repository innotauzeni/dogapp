using Dog.Application.Authorization;
using Dog.Application.Features.Countries.Commands;
using Dog.Application.Features.Countries.DTOs;
using Dog.Application.Features.Countries.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CityController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetCities")]
        [HasPermission("city.view")]
        public async Task<IActionResult> GetCities(int ProvinceId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetCitiesByProvinceQuery(ProvinceId), ct));


        [HttpPost("CreateCity")]
        [HasPermission("city.create")]
        public async Task<IActionResult> CreateCity([FromBody] CreateCityDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateCityCommand(dto), ct));


        [HttpPut("UpdateCity/{id:int}")]
        [HasPermission("city.edit")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CreateCityDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateCityCommand(id, dto), ct));
        }

        [HttpDelete("DeleteCity/{id:int}")]
        [HasPermission("city.delete")]
        public async Task<IActionResult> DeleteCity(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteCityCommand(id), ct));



    }

}
