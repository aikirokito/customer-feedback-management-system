using CFMS.Domain.Enums;
using CFMS.Domain.Rules;
using NUnit.Framework;

namespace CFMS.Lab3.NUnitTests.Lab3;

[TestFixture]
public class FeedbackStatusRulesNUnitTests
{
    [TestCase(FeedbackStatus.Submitted, FeedbackStatus.Assigned, true, TestName = "Function3_UTCID01_Normal_SubmittedToAssigned")]
    [TestCase(FeedbackStatus.Submitted, FeedbackStatus.Cancelled, true, TestName = "Function3_UTCID02_Normal_SubmittedToCancelled")]
    [TestCase(FeedbackStatus.Submitted, FeedbackStatus.InProgress, false, TestName = "Function3_UTCID03_Abnormal_SubmittedToInProgress")]
    [TestCase(FeedbackStatus.Assigned, FeedbackStatus.InProgress, true, TestName = "Function3_UTCID04_Normal_AssignedToInProgress")]
    [TestCase(FeedbackStatus.Assigned, FeedbackStatus.Cancelled, false, TestName = "Function3_UTCID05_Abnormal_AssignedToCancelled")]
    [TestCase(FeedbackStatus.InProgress, FeedbackStatus.Resolved, true, TestName = "Function3_UTCID06_Normal_InProgressToResolved")]
    [TestCase(FeedbackStatus.InProgress, FeedbackStatus.Closed, false, TestName = "Function3_UTCID07_Abnormal_InProgressToClosed")]
    [TestCase(FeedbackStatus.Resolved, FeedbackStatus.Closed, true, TestName = "Function3_UTCID08_Normal_ResolvedToClosed")]
    [TestCase(FeedbackStatus.Resolved, FeedbackStatus.InProgress, false, TestName = "Function3_UTCID09_Abnormal_ResolvedToInProgress")]
    [TestCase(FeedbackStatus.Cancelled, FeedbackStatus.Closed, false, TestName = "Function3_UTCID10_Abnormal_CancelledToClosed")]
    [TestCase(FeedbackStatus.Closed, FeedbackStatus.Resolved, false, TestName = "Function3_UTCID11_Abnormal_ClosedToResolved")]
    public void IsTransitionAllowed_ReturnsWorkbookExpected(
        FeedbackStatus from,
        FeedbackStatus to,
        bool expected)
    {
        var actual = FeedbackStatusRules.IsTransitionAllowed(from, to);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Function3_UTCID12_Boundary_ClosedHasNoAllowedTransitions()
    {
        var actual = FeedbackStatusRules.GetAllowedTransitions(FeedbackStatus.Closed);

        Assert.That(actual, Is.Empty);
    }

    [TestCase(FeedbackStatus.Cancelled, false, TestName = "Function3_UTCID13_Boundary_CancelledDoesNotRequireReason")]
    [TestCase(FeedbackStatus.Closed, true, TestName = "Function3_UTCID14_Boundary_ClosedRequiresReason")]
    [TestCase(FeedbackStatus.Assigned, false, TestName = "Function3_UTCID15_Boundary_AssignedDoesNotRequireReason")]
    public void RequiresReason_ReturnsWorkbookExpected(FeedbackStatus status, bool expected)
    {
        var actual = FeedbackStatusRules.RequiresReason(status);

        Assert.That(actual, Is.EqualTo(expected));
    }
}
