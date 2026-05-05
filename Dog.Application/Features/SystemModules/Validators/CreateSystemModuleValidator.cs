using FluentValidation;
using Dog.Application.Features.SystemModules.Commands;

namespace Dog.Application.Features.SystemModules.Validators
{
    public class CreateSystemModuleValidator : AbstractValidator<CreateSystemModuleCommand>
    {
        public CreateSystemModuleValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Dto.Icon).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateSystemModuleValidator : AbstractValidator<UpdateSystemModuleCommand>
    {
        public UpdateSystemModuleValidator()
        {
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Dto.Icon).NotEmpty().MaximumLength(100);
        }
    }
}
