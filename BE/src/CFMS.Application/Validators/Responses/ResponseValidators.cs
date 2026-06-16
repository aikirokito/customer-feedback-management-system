using CFMS.Application.DTOs.Responses;
using FluentValidation;

namespace CFMS.Application.Validators.Responses;

public class CreateResponseRequestValidator : AbstractValidator<CreateResponseRequest>
{
    public CreateResponseRequestValidator()
    {
        RuleFor(x => x.FeedbackId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}

public class UpdateResponseRequestValidator : AbstractValidator<UpdateResponseRequest>
{
    public UpdateResponseRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}
