using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Notifications;
using CFMS.Application.Services.Implementations;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Notifications;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendNotification_PersistsAndPublishesRealtimeEvent()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<INotificationRepository>();
        var realtime = new Mock<IRealTimeNotificationService>();
        var mapper = new Mock<IMapper>();
        Notification? saved = null;
        unitOfWork.SetupGet(x => x.Notifications).Returns(repository.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repository.Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => saved = notification)
            .Returns(Task.CompletedTask);
        mapper.Setup(x => x.Map<NotificationDto>(It.IsAny<object>())).Returns(new NotificationDto());
        realtime.Setup(x => x.SendNotificationToUserAsync(It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var userId = Guid.NewGuid();

        await new NotificationService(unitOfWork.Object, mapper.Object, realtime.Object)
            .SendNotificationAsync(userId, NotificationType.FeedbackSubmitted, "Received", "Your feedback was received.");

        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(userId);
        saved.IsRead.Should().BeFalse();
        realtime.Verify(x => x.SendNotificationToUserAsync(userId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_WhenOwnedByAnotherUser_IsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var notification = new Notification { Id = Guid.NewGuid(), UserId = ownerId };
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<INotificationRepository>();
        unitOfWork.SetupGet(x => x.Notifications).Returns(repository.Object);
        repository.Setup(x => x.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var action = () => new NotificationService(unitOfWork.Object, Mock.Of<IMapper>(), Mock.Of<IRealTimeNotificationService>())
            .MarkAsReadAsync(notification.Id, Guid.NewGuid());

        await action.Should().ThrowAsync<ForbiddenException>();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAllAsRead_UsesCurrentUserScopeAndSaves()
    {
        var userId = Guid.NewGuid();
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<INotificationRepository>();
        unitOfWork.SetupGet(x => x.Notifications).Returns(repository.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repository.Setup(x => x.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await new NotificationService(unitOfWork.Object, Mock.Of<IMapper>(), Mock.Of<IRealTimeNotificationService>()).MarkAllAsReadAsync(userId);

        repository.Verify(x => x.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
