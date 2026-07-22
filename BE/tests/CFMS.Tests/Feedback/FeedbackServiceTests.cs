using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Feedback;

public class FeedbackServiceTests
{
    [Theory]
    [InlineData(UserRole.Customer, true, true)]
    [InlineData(UserRole.Customer, false, false)]
    [InlineData(UserRole.SupportStaff, true, true)]
    [InlineData(UserRole.SupportStaff, false, false)]
    [InlineData(UserRole.DepartmentManager, true, true)]
    [InlineData(UserRole.DepartmentManager, false, true)]
    [InlineData(UserRole.SystemAdmin, false, true)]
    public async Task GetFeedbackById_EnforcesActorScope(UserRole role, bool scopeMatches, bool shouldSucceed)
    {
        var departmentId = Guid.NewGuid();
        var actor = new User
        {
            Id = Guid.NewGuid(),
            Role = role,
            Status = UserStatus.Active,
            DepartmentId = role == UserRole.DepartmentManager ? departmentId : null
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = role == UserRole.Customer && scopeMatches ? actor.Id : Guid.NewGuid(),
            AssignedToUserId = role == UserRole.SupportStaff && scopeMatches ? actor.Id : Guid.NewGuid(),
            DepartmentId = role == UserRole.DepartmentManager && scopeMatches ? departmentId : Guid.NewGuid(),
            Status = FeedbackStatus.InProgress
        };

        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        var mapper = new Mock<IMapper>();
        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        users.Setup(repository => repository.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);
        mapper.Setup(value => value.Map<FeedbackDetailDto>(feedback)).Returns(new FeedbackDetailDto());
        var service = new FeedbackService(
            unitOfWork.Object,
            mapper.Object,
            Mock.Of<INotificationService>(),
            Mock.Of<IAuditLogService>(),
            Mock.Of<ISupabaseStorageService>());

        var action = () => service.GetFeedbackByIdAsync(feedback.Id, actor.Id);

        if (shouldSucceed)
        {
            await action.Should().NotThrowAsync();
        }
        else
        {
            await action.Should().ThrowAsync<ForbiddenException>();
        }
    }

    [Fact]
    public async Task GetFeedbacks_ForManager_DoesNotApplyDepartmentScope()
    {
        var manager = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.DepartmentManager,
            Status = UserStatus.Active,
            DepartmentId = Guid.NewGuid()
        };
        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        var mapper = new Mock<IMapper>();
        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        users.Setup(repository => repository.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);
        feedbacks.Setup(repository => repository.GetPagedAsync(
                1, 20, null, null, null, null, null, null, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Domain.Entities.Feedback>(), 0));
        mapper.Setup(value => value.Map<IEnumerable<FeedbackListItemDto>>(It.IsAny<object>()))
            .Returns(Array.Empty<FeedbackListItemDto>());
        var service = new FeedbackService(
            unitOfWork.Object,
            mapper.Object,
            Mock.Of<INotificationService>(),
            Mock.Of<IAuditLogService>(),
            Mock.Of<ISupabaseStorageService>());

        await service.GetFeedbacksAsync(new FeedbackFilterRequest(), manager.Id);

        feedbacks.Verify(repository => repository.GetPagedAsync(
            1, 20, null, null, null, null, null, null, null, null, null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFeedback_SupportStaffCannotMoveTicketToAnotherDepartment()
    {
        var currentDepartmentId = Guid.NewGuid();
        var staff = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active,
            DepartmentId = currentDepartmentId
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            AssignedToUserId = staff.Id,
            DepartmentId = currentDepartmentId,
            CategoryId = Guid.NewGuid(),
            Status = FeedbackStatus.InProgress
        };
        var otherDepartment = new Department { Id = Guid.NewGuid(), Name = "Other", IsActive = true };
        var otherCategory = new FeedbackCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Other category",
            DepartmentId = otherDepartment.Id,
            Department = otherDepartment,
            IsActive = true
        };
        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        var categories = new Mock<IFeedbackCategoryRepository>();
        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.FeedbackCategories).Returns(categories.Object);
        feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        users.Setup(repository => repository.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        categories.Setup(repository => repository.GetByIdAsync(otherCategory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherCategory);
        var service = new FeedbackService(
            unitOfWork.Object,
            Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IAuditLogService>(),
            Mock.Of<ISupabaseStorageService>());

        var action = () => service.UpdateFeedbackAsync(feedback.Id, new UpdateFeedbackRequest
        {
            Title = "Updated",
            Description = "Updated description",
            CategoryId = otherCategory.Id,
            Priority = FeedbackPriority.High
        }, staff.Id);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*own department*");
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_WhenResolvedFeedbackIsReopened_RejectsInvalidTransition()
    {
        var staff = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Reopened issue",
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = staff.Id,
            Status = FeedbackStatus.Resolved,
            ResolvedAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        var notifications = new Mock<INotificationService>();
        var auditLogs = new Mock<IAuditLogService>();
        unitOfWork.SetupGet(x => x.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(x => x.Users).Returns(users.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        users.Setup(x => x.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        notifications.Setup(x => x.SendNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        auditLogs.Setup(x => x.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FeedbackService(
            unitOfWork.Object,
            Mock.Of<IMapper>(),
            notifications.Object,
            auditLogs.Object,
            Mock.Of<ISupabaseStorageService>());

        var action = () => service.ChangeStatusAsync(feedback.Id, new ChangeFeedbackStatusRequest
        {
            NewStatus = FeedbackStatus.InProgress
        }, staff.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Resolved*InProgress*");
        feedback.Status.Should().Be(FeedbackStatus.Resolved);
    }

    [Fact]
    public async Task ChangeStatus_ToResolvedWithOnlyCustomerComment_RejectsWithoutChangingAggregateOrSaving()
    {
        var staff = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active
        };
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.Customer,
            Status = UserStatus.Active
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Needs an official response",
            SubmittedByUserId = customer.Id,
            AssignedToUserId = staff.Id,
            Status = FeedbackStatus.InProgress
        };
        feedback.Comments.Add(new FeedbackComment
        {
            FeedbackId = feedback.Id,
            AuthorUserId = customer.Id,
            AuthorUser = customer,
            Content = "Legacy discussion comment"
        });

        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        users.Setup(repository => repository.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        var service = new FeedbackService(
            unitOfWork.Object,
            Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IAuditLogService>(),
            Mock.Of<ISupabaseStorageService>());

        var action = () => service.ChangeStatusAsync(feedback.Id, new ChangeFeedbackStatusRequest
        {
            NewStatus = FeedbackStatus.Resolved
        }, staff.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Staff response*");
        feedback.Status.Should().Be(FeedbackStatus.InProgress);
        feedback.ResolvedAtUtc.Should().BeNull();
        feedback.StatusHistory.Should().BeEmpty();
        feedbacks.Verify(repository => repository.AddStatusHistoryAsync(
            It.IsAny<FeedbackStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_ToResolvedWithPublicStaffResponse_UpdatesStatusAndSavesHistory()
    {
        var staff = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Ready to resolve",
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = staff.Id,
            Status = FeedbackStatus.InProgress
        };
        feedback.Responses.Add(new FeedbackResponse
        {
            FeedbackId = feedback.Id,
            RespondedByUserId = staff.Id,
            RespondedByUser = staff,
            Content = "The issue has been handled.",
            IsInternal = false
        });

        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        users.Setup(repository => repository.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        var service = new FeedbackService(
            unitOfWork.Object,
            Mock.Of<IMapper>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IAuditLogService>(),
            Mock.Of<ISupabaseStorageService>());

        await service.ChangeStatusAsync(feedback.Id, new ChangeFeedbackStatusRequest
        {
            NewStatus = FeedbackStatus.Resolved
        }, staff.Id);

        feedback.Status.Should().Be(FeedbackStatus.Resolved);
        feedback.ResolvedAtUtc.Should().NotBeNull();
        feedback.StatusHistory.Should().ContainSingle(history =>
            history.FromStatus == FeedbackStatus.InProgress &&
            history.ToStatus == FeedbackStatus.Resolved &&
            history.ChangedByUserId == staff.Id);
        feedbacks.Verify(repository => repository.AddStatusHistoryAsync(
            It.Is<FeedbackStatusHistory>(history =>
                history.FeedbackId == feedback.Id &&
                history.FromStatus == FeedbackStatus.InProgress &&
                history.ToStatus == FeedbackStatus.Resolved),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

}
