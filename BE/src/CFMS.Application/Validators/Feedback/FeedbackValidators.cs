using CFMS.Application.DTOs.Feedback;
using CFMS.Domain.Constants;
using FluentValidation;

namespace CFMS.Application.Validators.Feedback;

public class CreateFeedbackRequestValidator : AbstractValidator<CreateFeedbackRequest>
{
    public CreateFeedbackRequestValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Title is required.")
            .Must(value => value.Trim().Length >= FeedbackConstants.TitleMinLength)
            .WithMessage($"Title must be at least {FeedbackConstants.TitleMinLength} characters.")
            .Must(value => value.Trim().Length <= FeedbackConstants.TitleMaxLength)
            .WithMessage($"Title must not exceed {FeedbackConstants.TitleMaxLength} characters.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Description is required.")
            .Must(value => value.Trim().Length >= FeedbackConstants.DescriptionMinLength)
            .WithMessage($"Description must be at least {FeedbackConstants.DescriptionMinLength} characters.")
            .Must(value => value.Trim().Length <= FeedbackConstants.DescriptionMaxLength)
            .WithMessage($"Description must not exceed {FeedbackConstants.DescriptionMaxLength} characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Rating)
            .NotNull().WithMessage("Rating is required.")
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
    }
}

public class UpdateFeedbackRequestValidator : AbstractValidator<UpdateFeedbackRequest>
{
    public UpdateFeedbackRequestValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Title is required.")
            .Must(value => value.Trim().Length >= FeedbackConstants.TitleMinLength)
            .WithMessage($"Title must be at least {FeedbackConstants.TitleMinLength} characters.")
            .Must(value => value.Trim().Length <= FeedbackConstants.TitleMaxLength)
            .WithMessage($"Title must not exceed {FeedbackConstants.TitleMaxLength} characters.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Description is required.")
            .Must(value => value.Trim().Length >= FeedbackConstants.DescriptionMinLength)
            .WithMessage($"Description must be at least {FeedbackConstants.DescriptionMinLength} characters.")
            .Must(value => value.Trim().Length <= FeedbackConstants.DescriptionMaxLength)
            .WithMessage($"Description must not exceed {FeedbackConstants.DescriptionMaxLength} characters.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required.");
        RuleFor(x => x.Rating)
            .NotNull().WithMessage("Rating is required.")
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
    }
}

public class UpdateFeedbackPriorityRequestValidator : AbstractValidator<UpdateFeedbackPriorityRequest>
{
    public UpdateFeedbackPriorityRequestValidator()
    {
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class ChangeFeedbackStatusRequestValidator : AbstractValidator<ChangeFeedbackStatusRequest>
{
    public ChangeFeedbackStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum().WithMessage("Invalid feedback status.");
        RuleFor(x => x.Reason)
            .NotEmpty().When(x => CFMS.Domain.Rules.FeedbackStatusRules.RequiresReason(x.NewStatus))
            .WithMessage("A reason is required when feedback is Closed.")
            .MaximumLength(500);
    }
}
