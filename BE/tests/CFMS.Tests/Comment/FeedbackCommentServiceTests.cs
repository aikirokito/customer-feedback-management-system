using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Comments;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Comment;

public class FeedbackCommentServiceTests
{
    [Fact]
    public async Task CreateComment_CustomerOwnSubmittedFeedbackSucceeds()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = customer.Id,
            Status = FeedbackStatus.Submitted
        };
        var fixture = CreateFixture(feedback, customer);

        await fixture.Service.CreateCommentAsync(new CreateCommentRequest
        {
            FeedbackId = feedback.Id,
            Content = "A valid Submitted feedback comment."
        }, customer.Id);

        feedback.Comments.Should().ContainSingle(comment => comment.AuthorUserId == customer.Id);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(FeedbackStatus.Assigned)]
    [InlineData(FeedbackStatus.InProgress)]
    [InlineData(FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.Closed)]
    [InlineData(FeedbackStatus.Cancelled)]
    public async Task CreateComment_CustomerNonSubmittedFeedbackIsReadOnly(FeedbackStatus status)
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = customer.Id,
            Status = status
        };
        var fixture = CreateFixture(feedback, customer);

        var action = () => fixture.Service.CreateCommentAsync(new CreateCommentRequest
        {
            FeedbackId = feedback.Id,
            Content = "This mutation must be rejected."
        }, customer.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*SUBMITTED*");
        feedback.Comments.Should().BeEmpty();
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(UserRole.DepartmentManager)]
    [InlineData(UserRole.SystemAdmin)]
    public async Task CreateComment_InternalOperationalRoleIsRejected(UserRole role)
    {
        var actor = new User { Id = Guid.NewGuid(), Role = role, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            Status = FeedbackStatus.Submitted
        };
        var fixture = CreateFixture(feedback, actor);

        var action = () => fixture.Service.CreateCommentAsync(new CreateCommentRequest
        {
            FeedbackId = feedback.Id,
            Content = "Managers and Admins cannot add discussion comments."
        }, actor.Id);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Only Customers*");
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateComment_WithParentFromAnotherFeedback_IsRejected()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = customer.Id
        };
        var foreignParent = new FeedbackComment
        {
            Id = Guid.NewGuid(),
            FeedbackId = Guid.NewGuid(),
            AuthorUserId = customer.Id,
            Content = "Other thread"
        };

        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        unitOfWork.SetupGet(x => x.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(x => x.Users).Returns(users.Object);
        feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        feedbacks.Setup(x => x.GetCommentByIdAsync(foreignParent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignParent);
        users.Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var service = new FeedbackCommentService(
            unitOfWork.Object,
            Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IAuditLogService>());

        var action = () => service.CreateCommentAsync(new CreateCommentRequest
        {
            FeedbackId = feedback.Id,
            ParentCommentId = foreignParent.Id,
            Content = "Invalid reply"
        }, customer.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*same feedback*");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateComment_WhenRouteFeedbackDoesNotMatch_IsNotFound()
    {
        var comment = new FeedbackComment
        {
            Id = Guid.NewGuid(),
            FeedbackId = Guid.NewGuid(),
            Feedback = new Domain.Entities.Feedback(),
            AuthorUserId = Guid.NewGuid(),
            Content = "Original"
        };
        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        unitOfWork.SetupGet(x => x.Feedbacks).Returns(feedbacks.Object);
        feedbacks.Setup(x => x.GetCommentByIdAsync(comment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(comment);
        var service = new FeedbackCommentService(unitOfWork.Object, Mock.Of<IMapper>(), Mock.Of<INotificationService>(), Mock.Of<IAuditLogService>());

        var action = () => service.UpdateCommentAsync(Guid.NewGuid(), comment.Id, new UpdateCommentRequest { Content = "Changed" }, comment.AuthorUserId);

        await action.Should().ThrowAsync<NotFoundException>();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CommentFixture CreateFixture(Domain.Entities.Feedback feedback, User actor)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        var mapper = new Mock<IMapper>();
        var auditLogs = new Mock<IAuditLogService>();
        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(
                feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        users.Setup(repository => repository.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);
        feedbacks.Setup(repository => repository.AddCommentAsync(
                It.IsAny<FeedbackComment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mapper.Setup(value => value.Map<CommentDto>(It.IsAny<FeedbackComment>()))
            .Returns(new CommentDto());
        auditLogs.Setup(value => value.LogAsync(
                It.IsAny<Guid?>(), It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FeedbackCommentService(
            unitOfWork.Object,
            mapper.Object,
            Mock.Of<INotificationService>(),
            auditLogs.Object);
        return new CommentFixture(service, unitOfWork);
    }

    private sealed record CommentFixture(
        FeedbackCommentService Service,
        Mock<IUnitOfWork> UnitOfWork);
}
