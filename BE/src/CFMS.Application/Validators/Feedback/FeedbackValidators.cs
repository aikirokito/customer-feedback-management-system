using CFMS.Application.DTOs.Feedback;
using CFMS.Domain.Constants;
using FluentValidation;

namespace CFMS.Application.Validators.Feedback;

public class CreateFeedbackRequestValidator : AbstractValidator<CreateFeedbackRequest>
{
    public CreateFeedbackRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(FeedbackConstants.TitleMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(FeedbackConstants.DescriptionMaxLength);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");
    }
}

public class UpdateFeedbackRequestValidator : AbstractValidator<UpdateFeedbackRequest>
{
    public UpdateFeedbackRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(FeedbackConstants.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(FeedbackConstants.DescriptionMaxLength);
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required.");
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class UpdateFeedbackPriorityRequestValidator : AbstractValidator<UpdateFeedbackPriorityRequest>
{
    public UpdateFeedbackPriorityRequestValidator()
    {
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class RateFeedbackRequestValidator : AbstractValidator<RateFeedbackRequest>
{
    public RateFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");
    }
}

public class ChangeFeedbackStatusRequestValidator : AbstractValidator<ChangeFeedbackStatusRequest>
{
    public ChangeFeedbackStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum().WithMessage("Invalid feedback status.");
        RuleFor(x => x.Reason)
            .NotEmpty().When(x => CFMS.Domain.Rules.FeedbackStatusRules.RequiresReason(x.NewStatus))
            .WithMessage("A reason is required when feedback is Rejected or Closed.")
            .MaximumLength(500);
    }
}
