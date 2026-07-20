using CFMS.Application.DTOs.Departments;
using FluentValidation;

namespace CFMS.Application.Validators.Departments;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name != null || x.Description != null || x.ClearDescription || x.IsActive.HasValue)
            .WithMessage("At least one department field must be supplied.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name != null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x)
            .Must(x => !(x.ClearDescription && x.Description != null))
            .WithMessage("Description and ClearDescription cannot be supplied together.");
    }
}
