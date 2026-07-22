using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.DTOs.Responses;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Response;

public class FeedbackResponseActionGuardTests
{
    [Theory]
    [InlineData(FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.Closed)]
    [InlineData(FeedbackStatus.Cancelled)]
    public async Task CreateResponse_ReadOnlyStatusIsRejectedWithoutSaving(FeedbackStatus status)
    {
        var staff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var feedback = CreateFeedback(staff.Id, status);
        var fixture = CreateFixture(feedback, staff);

        var action = () => fixture.Service.CreateResponseAsync(ValidRequest(feedback.Id), staff.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*ASSIGNED or IN_PROGRESS*");
        feedback.Responses.Should().BeEmpty();
        fixture.Feedbacks.Verify(repository => repository.AddResponseAsync(
            It.IsAny<FeedbackResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(FeedbackStatus.Assigned)]
    [InlineData(FeedbackStatus.InProgress)]
    public async Task CreateResponse_AssignedStaffOnActionableStatusSucceeds(FeedbackStatus status)
    {
        var staff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var feedback = CreateFeedback(staff.Id, status);
        var fixture = CreateFixture(feedback, staff);

        await fixture.Service.CreateResponseAsync(ValidRequest(feedback.Id), staff.Id);

        feedback.Responses.Should().ContainSingle(response => response.RespondedByUserId == staff.Id);
        fixture.Feedbacks.Verify(repository => repository.AddResponseAsync(
            It.Is<FeedbackResponse>(response => response.FeedbackId == feedback.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(UserRole.DepartmentManager)]
    [InlineData(UserRole.SystemAdmin)]
    public async Task CreateResponse_NonStaffOperationalRoleIsRejectedWithoutSaving(UserRole role)
    {
        var actor = new User { Id = Guid.NewGuid(), Role = role, Status = UserStatus.Active };
        var feedback = CreateFeedback(Guid.NewGuid(), FeedbackStatus.Assigned);
        var fixture = CreateFixture(feedback, actor);

        var action = () => fixture.Service.CreateResponseAsync(ValidRequest(feedback.Id), actor.Id);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Only Support Staff*");
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Domain.Entities.Feedback CreateFeedback(Guid staffId, FeedbackStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Action guard feedback",
        SubmittedByUserId = Guid.NewGuid(),
        AssignedToUserId = staffId,
        Status = status
    };

    private static CreateResponseRequest ValidRequest(Guid feedbackId) => new()
    {
        FeedbackId = feedbackId,
        Content = "A valid public Staff response."
    };

    private static ResponseFixture CreateFixture(Domain.Entities.Feedback feedback, User actor)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        var mapper = new Mock<IMapper>();
        var notifications = new Mock<INotificationService>();
        var auditLogs = new Mock<IAuditLogService>();
        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        feedbacks.Setup(repository => repository.AddResponseAsync(
                It.IsAny<FeedbackResponse>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        users.Setup(repository => repository.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);
        mapper.Setup(value => value.Map<FeedbackResponseDto>(It.IsAny<FeedbackResponse>()))
            .Returns(new FeedbackResponseDto());
        notifications.Setup(value => value.SendNotificationAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogs.Setup(value => value.LogAsync(
                It.IsAny<Guid?>(), It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FeedbackResponseService(
            unitOfWork.Object, mapper.Object, notifications.Object, auditLogs.Object);
        return new ResponseFixture(service, unitOfWork, feedbacks);
    }

    private sealed record ResponseFixture(
        FeedbackResponseService Service,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IFeedbackRepository> Feedbacks);
}
