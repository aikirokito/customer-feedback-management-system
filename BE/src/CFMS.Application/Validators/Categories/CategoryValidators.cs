using CFMS.Application.DTOs.Categories;
using FluentValidation;

namespace CFMS.Application.Validators.Categories;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Name) ||
                       x.Description != null ||
                       x.IsActive.HasValue ||
                       x.DepartmentId.HasValue ||
                       x.ClearDepartment)
            .WithMessage("At least one category field must be provided.");
        RuleFor(x => x)
            .Must(x => !(x.DepartmentId.HasValue && x.ClearDepartment))
            .WithMessage("DepartmentId and ClearDepartment cannot be provided together.");
    }
}
