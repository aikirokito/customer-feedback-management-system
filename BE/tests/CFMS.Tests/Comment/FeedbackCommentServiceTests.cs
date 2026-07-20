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
}
