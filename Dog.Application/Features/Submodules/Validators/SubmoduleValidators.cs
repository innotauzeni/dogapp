using FluentValidation;
using Dog.Application.Features.Submodules.Commands;

namespace Dog.Application.Features.Submodules.Validators
{
    public class CreateSubmoduleValidator : AbstractValidator<CreateSubmoduleCommand>
    {
        public CreateSubmoduleValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Dto.ModuleId).GreaterThan(0);
            RuleFor(x => x.Dto.Route).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.AccessName).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateSubmoduleValidator : AbstractValidator<UpdateSubmoduleCommand>
    {
        public UpdateSubmoduleValidator()
        {
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Dto.ModuleId).GreaterThan(0);
            RuleFor(x => x.Dto.Route).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.AccessName).NotEmpty().MaximumLength(100);
        }
    }
}
