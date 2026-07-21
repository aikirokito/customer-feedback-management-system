using CFMS.Domain.Enums;
using CFMS.Domain.Rules;
using Xunit;

namespace CFMS.Tests.Feedback;

/// <summary>
/// Unit tests for the SRS-defined FeedbackStatusRules state machine.
/// </summary>
public class FeedbackStatusRulesTests
{
    [Theory]
    [InlineData(FeedbackStatus.Submitted, FeedbackStatus.Assigned, true)]
    [InlineData(FeedbackStatus.Submitted, FeedbackStatus.Cancelled, true)]
    [InlineData(FeedbackStatus.Submitted, FeedbackStatus.InProgress, false)]
    [InlineData(FeedbackStatus.Assigned, FeedbackStatus.InProgress, true)]
    [InlineData(FeedbackStatus.Assigned, FeedbackStatus.Cancelled, false)]
    [InlineData(FeedbackStatus.InProgress, FeedbackStatus.Resolved, true)]
    [InlineData(FeedbackStatus.InProgress, FeedbackStatus.Closed, false)]
    [InlineData(FeedbackStatus.Resolved, FeedbackStatus.Closed, true)]
    [InlineData(FeedbackStatus.Resolved, FeedbackStatus.InProgress, false)]
    [InlineData(FeedbackStatus.Cancelled, FeedbackStatus.Closed, false)]
    [InlineData(FeedbackStatus.Closed, FeedbackStatus.Resolved, false)]
    public void IsTransitionAllowed_ReturnsExpectedResult(FeedbackStatus from, FeedbackStatus to, bool expected)
    {
        var result = FeedbackStatusRules.IsTransitionAllowed(from, to);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Closed_IsTerminalState_HasNoAllowedTransitions()
    {
        var transitions = FeedbackStatusRules.GetAllowedTransitions(FeedbackStatus.Closed);
        Assert.Empty(transitions);
    }

    [Theory]
    [InlineData(FeedbackStatus.Cancelled, false)]
    [InlineData(FeedbackStatus.Closed, true)]
    [InlineData(FeedbackStatus.Assigned, false)]
    public void RequiresReason_ReturnsExpectedResult(FeedbackStatus status, bool expected)
    {
        Assert.Equal(expected, FeedbackStatusRules.RequiresReason(status));
    }
}
