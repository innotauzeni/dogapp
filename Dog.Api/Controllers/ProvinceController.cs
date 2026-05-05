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
    public class ProvinceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProvinceController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetProvinces")]
        [HasPermission("province.view")]
        public async Task<IActionResult> GetProvinces(int countryId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetProvincesByCountryQuery(countryId), ct));


        [HttpPost("CreateProvince")]
        [HasPermission("province.create")]
        public async Task<IActionResult> CreateProvince([FromBody] CreateProvinceDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateProvinceCommand(dto), ct));


        [HttpPut("UpdateProvince/{id:int}")]
        [HasPermission("province.edit")]
        public async Task<IActionResult> UpdateProvince(int id, [FromBody] CreateProvinceDto dto, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new UpdateProvinceCommand(id, dto), ct));
        }

        [HttpDelete("DeleteProvince/{id:int}")]
        [HasPermission("province.delete")]
        public async Task<IActionResult> DeleteProvince(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteProvinceCommand(id), ct));



    }


}
