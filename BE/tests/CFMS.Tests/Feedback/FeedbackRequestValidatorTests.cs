using CFMS.Application.DTOs.Feedback;
using CFMS.Application.Validators.Feedback;
using CFMS.Domain.Enums;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace CFMS.Tests.Feedback;

public class FeedbackRequestValidatorTests
{
    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void CreateAndUpdateValidators_EnforceTrimmedTitleLength(int length, bool shouldBeValid)
    {
        var results = ValidateBoth(new string('T', length), new string('D', 10));

        foreach (var result in results)
        {
            var titleErrors = result.Errors.Where(error => error.PropertyName == nameof(CreateFeedbackRequest.Title));
            if (shouldBeValid)
                titleErrors.Should().BeEmpty();
            else
                titleErrors.Should().ContainSingle();
        }
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void CreateAndUpdateValidators_EnforceTrimmedDescriptionLength(int length, bool shouldBeValid)
    {
        var results = ValidateBoth(new string('T', 5), new string('D', length));

        foreach (var result in results)
        {
            var descriptionErrors = result.Errors.Where(error => error.PropertyName == nameof(CreateFeedbackRequest.Description));
            if (shouldBeValid)
                descriptionErrors.Should().BeEmpty();
            else
                descriptionErrors.Should().ContainSingle();
        }
    }

    [Fact]
    public void CreateAndUpdateValidators_RejectWhitespaceOnlyValues()
    {
        var results = ValidateBoth(" \t ", " \r\n ");

        foreach (var result in results)
        {
            result.Errors.Should().ContainSingle(error =>
                error.PropertyName == nameof(CreateFeedbackRequest.Title) &&
                error.ErrorMessage == "Title is required.");
            result.Errors.Should().ContainSingle(error =>
                error.PropertyName == nameof(CreateFeedbackRequest.Description) &&
                error.ErrorMessage == "Description is required.");
        }
    }

    [Fact]
    public void CreateAndUpdateValidators_UseTrimmedLengthsAtBoundaries()
    {
        var paddedValidResults = ValidateBoth(
            $"  {new string('T', 5)}  ",
            $"  {new string('D', 2000)}  ");
        var paddedInvalidResults = ValidateBoth("  Test  ", $"  {new string('D', 9)}  ");

        paddedValidResults.Should().OnlyContain(result => result.IsValid);
        foreach (var result in paddedInvalidResults)
        {
            result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateFeedbackRequest.Title));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateFeedbackRequest.Description));
        }
    }

    private static ValidationResult[] ValidateBoth(string title, string description)
    {
        var categoryId = Guid.NewGuid();
        return
        [
            new CreateFeedbackRequestValidator().Validate(new CreateFeedbackRequest
            {
                Title = title,
                Description = description,
                CategoryId = categoryId
            }),
            new UpdateFeedbackRequestValidator().Validate(new UpdateFeedbackRequest
            {
                Title = title,
                Description = description,
                CategoryId = categoryId,
                Priority = FeedbackPriority.Medium
            })
        ];
    }
}
