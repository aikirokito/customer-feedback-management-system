using CFMS.Application.DTOs.Assignments;
using FluentValidation;

namespace CFMS.Application.Validators.Assignments;

public class AssignFeedbackRequestValidator : AbstractValidator<AssignFeedbackRequest>
{
    public AssignFeedbackRequestValidator()
    {
        RuleFor(x => x.FeedbackId).NotEmpty();
        RuleFor(x => x.AssignToUserId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000).When(x => x.Note != null);
    }
}
