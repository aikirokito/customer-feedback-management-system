using CFMS.Application.DTOs.Assignments;
using CFMS.Application.DTOs.Responses;
using CFMS.Application.Validators.Assignments;
using CFMS.Application.Validators.Responses;
using FluentAssertions;
using Xunit;

namespace CFMS.Tests.Api;

public class RouteBoundFeedbackIdValidationTests
{
    [Fact]
    public void AssignOrReassignBody_WithoutFeedbackId_PassesAutomaticValidation()
    {
        var request = new AssignFeedbackRequest
        {
            AssignToUserId = Guid.NewGuid(),
            Note = "Please investigate"
        };

        var result = new AssignFeedbackRequestValidator().Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResponseBody_WithoutFeedbackId_PassesAutomaticValidation()
    {
        var request = new CreateResponseRequest
        {
            Content = "We are reviewing your feedback."
        };

        var result = new CreateResponseRequestValidator().Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AssignmentBody_StillRequiresAnAssignee()
    {
        var request = new AssignFeedbackRequest();

        var result = new AssignFeedbackRequestValidator().Validate(request);

        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(AssignFeedbackRequest.AssignToUserId));
    }

    [Fact]
    public void ResponseBody_StillRequiresContent()
    {
        var request = new CreateResponseRequest();

        var result = new CreateResponseRequestValidator().Validate(request);

        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(CreateResponseRequest.Content));
    }
}
