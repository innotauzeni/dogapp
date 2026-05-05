using FluentValidation;
using Dog.Application.Features.Permissions.Commands;

namespace Dog.Application.Features.Permissions.Validators
{
    public class CreatePermissionValidator : AbstractValidator<CreatePermissionCommand>
    {
        public CreatePermissionValidator()
        {
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100)
                .Matches(@"^[a-z]+\.[a-z]+$").WithMessage("Name must follow pattern 'module.action' e.g. 'events.create'");
            RuleFor(x => x.Dto.DisplayName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Dto.Module).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdatePermissionValidator : AbstractValidator<UpdatePermissionCommand>
    {
        public UpdatePermissionValidator()
        {
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Dto.DisplayName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Dto.Module).NotEmpty().MaximumLength(100);
        }
    }
}
