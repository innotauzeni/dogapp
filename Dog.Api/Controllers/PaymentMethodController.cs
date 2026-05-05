using Dog.Application.Authorization;
using Dog.Application.Features.Countries.Commands;
using Dog.Application.Features.Countries.DTOs;
using Dog.Application.Features.Countries.Queries;
using Dog.Application.Features.Currencies.Commands;
using Dog.Application.Features.PaymentMethods.Commands;
using Dog.Application.Features.PaymentMethods.DTOs;
using Dog.Application.Features.PaymentMethods.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentMethodController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetPaymentMethods")]
        [HasPermission("paymentmethod.view")]
        public async Task<IActionResult> GetPaymentMethods(int CompanyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetPaymentMethodsByCompanyQuery(CompanyId), ct));


        [HttpPost("CreatePaymentMethod")]
        [HasPermission("paymentmethod.create")]
        public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreatePaymentMethodCommand(dto), ct));


        [HttpPut("UpdatePaymentMethod/{id:int}")]
        [HasPermission("paymentmethod.edit")]
        public async Task<IActionResult> UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdatePaymentMethodCommand(dto), ct));
        }

        [HttpDelete("DeletePaymentMethod/{id:int}")]
        [HasPermission("paymentmethod.delete")]
        public async Task<IActionResult> DeletePaymentMethod(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeletePaymentMethodCommand(id), ct));



        [HttpPost("PaymentMethod/{id:int}/toggle")]
        [HasPermission("paymentmethod.edit")]
        public async Task<IActionResult> ToggleCurrency(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new TogglePaymentMethodCommand(id), ct));

    }


}
