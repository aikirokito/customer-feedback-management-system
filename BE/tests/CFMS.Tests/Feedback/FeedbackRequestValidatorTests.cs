using AutoMapper;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.Mappings;
using CFMS.Application.Validators.Feedback;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace CFMS.Tests.Feedback;

public class FeedbackRequestValidatorTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void CreateValidator_RequiresRatingFromOneThroughFive(int? rating, bool shouldBeValid)
    {
        var result = new CreateFeedbackRequestValidator().Validate(new CreateFeedbackRequest
        {
            Title = "Valid title",
            Description = "Valid description",
            CategoryId = Guid.NewGuid(),
            Rating = rating
        });

        if (shouldBeValid)
            result.Errors.Should().NotContain(error => error.PropertyName == nameof(CreateFeedbackRequest.Rating));
        else
            result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateFeedbackRequest.Rating));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void UpdateValidator_RequiresRatingFromOneThroughFive(int? rating, bool shouldBeValid)
    {
        var result = new UpdateFeedbackRequestValidator().Validate(new UpdateFeedbackRequest
        {
            Title = "Valid title",
            Description = "Valid description",
            CategoryId = Guid.NewGuid(),
            Rating = rating
        });

        if (shouldBeValid)
            result.Errors.Should().NotContain(error => error.PropertyName == nameof(UpdateFeedbackRequest.Rating));
        else
            result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateFeedbackRequest.Rating));
    }

    [Fact]
    public void CreateRequest_WithNonIntegerRating_CannotBeDeserialized()
    {
        const string json = """{"title":"Valid title","description":"Valid description","categoryId":"00000000-0000-0000-0000-000000000001","rating":1.5}""";

        var action = () => JsonSerializer.Deserialize<CreateFeedbackRequest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void DetailMapping_PreservesHistoricalNullRating()
    {
        var customer = new User { FirstName = "Historical", LastName = "Customer" };
        var category = new FeedbackCategoryEntity { Name = "Legacy" };
        var feedback = new Domain.Entities.Feedback
        {
            Title = "Legacy feedback",
            Description = "Historical feedback without rating",
            SubmittedByUser = customer,
            Category = category,
            Rating = null
        };
        var configuration = new MapperConfiguration(
            config => config.AddProfile<FeedbackMappingProfile>(),
            NullLoggerFactory.Instance);

        var result = configuration.CreateMapper().Map<FeedbackDetailDto>(feedback);

        result.Rating.Should().BeNull();
    }

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
                CategoryId = categoryId,
                Rating = 3
            }),
            new UpdateFeedbackRequestValidator().Validate(new UpdateFeedbackRequest
            {
                Title = title,
                Description = description,
                CategoryId = categoryId,
                Rating = 3
            })
        ];
    }
}
