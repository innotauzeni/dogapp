using MediatR;
using Dog.Application.Common;
using Dog.Application.Features.Onboarding.DTOs;

namespace Dog.Application.Features.Onboarding.Commands
{
    public record OnboardCompanyCommand(OnboardRequestDto Dto) : IRequest<ApiResponse<OnboardResultDto>>;
}
