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
    public async Task UpdateFeedback_OwnerUpdatesOwnSubmittedFeedback_PersistsEditableFieldsOnly()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var originalOwnerId = customer.Id;
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Original title",
            Description = "Original description",
            SubmittedByUserId = originalOwnerId,
            CategoryId = Guid.NewGuid(),
            Rating = 2,
            Status = FeedbackStatus.Submitted
        };
        var category = new FeedbackCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Updated category",
            IsActive = true
        };
        var fixture = CreateUpdateFixture(feedback, customer, category);

        await fixture.Service.UpdateFeedbackAsync(feedback.Id, new UpdateFeedbackRequest
        {
            Title = "  Updated title  ",
            Description = "  Updated description  ",
            CategoryId = category.Id,
            Rating = 5
        }, customer.Id);

        feedback.Title.Should().Be("Updated title");
        feedback.Description.Should().Be("Updated description");
        feedback.CategoryId.Should().Be(category.Id);
        feedback.Rating.Should().Be(5);
        feedback.Status.Should().Be(FeedbackStatus.Submitted);
        feedback.SubmittedByUserId.Should().Be(originalOwnerId);
        feedback.StatusHistory.Should().BeEmpty();
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFeedback_AnotherCustomerIsRejectedWithoutSaving()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Original title",
            Description = "Original description",
            SubmittedByUserId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Rating = 2,
            Status = FeedbackStatus.Submitted
        };
        var category = new FeedbackCategoryEntity { Id = Guid.NewGuid(), IsActive = true };
        var fixture = CreateUpdateFixture(feedback, customer, category);

        var action = () => fixture.Service.UpdateFeedbackAsync(feedback.Id, ValidUpdate(category.Id), customer.Id);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*own feedback*");
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(FeedbackStatus.Assigned)]
    [InlineData(FeedbackStatus.InProgress)]
    [InlineData(FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.Closed)]
    [InlineData(FeedbackStatus.Cancelled)]
    public async Task UpdateFeedback_NonSubmittedStatusIsRejectedWithoutMutationOrSave(FeedbackStatus status)
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Original title",
            Description = "Original description",
            SubmittedByUserId = customer.Id,
            CategoryId = Guid.NewGuid(),
            Rating = 2,
            Status = status
        };
        var category = new FeedbackCategoryEntity { Id = Guid.NewGuid(), IsActive = true };
        var fixture = CreateUpdateFixture(feedback, customer, category);

        var action = () => fixture.Service.UpdateFeedbackAsync(feedback.Id, ValidUpdate(category.Id), customer.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*SUBMITTED*");
        feedback.Title.Should().Be("Original title");
        feedback.Description.Should().Be("Original description");
        feedback.Rating.Should().Be(2);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(6)]
    public async Task UpdateFeedback_InvalidRatingIsRejectedWithoutSaving(int? rating)
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = customer.Id,
            Status = FeedbackStatus.Submitted
        };
        var category = new FeedbackCategoryEntity { Id = Guid.NewGuid(), IsActive = true };
        var fixture = CreateUpdateFixture(feedback, customer, category);
        var request = ValidUpdate(category.Id);
        request.Rating = rating;

        var action = () => fixture.Service.UpdateFeedbackAsync(feedback.Id, request, customer.Id);

        await action.Should().ThrowAsync<ValidationException>();
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFeedback_InactiveCategoryIsRejectedWithoutSaving()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = customer.Id,
            Status = FeedbackStatus.Submitted
        };
        var category = new FeedbackCategoryEntity { Id = Guid.NewGuid(), IsActive = false };
        var fixture = CreateUpdateFixture(feedback, customer, category);

        var action = () => fixture.Service.UpdateFeedbackAsync(feedback.Id, ValidUpdate(category.Id), customer.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Disabled categories*");
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFeedback_MissingCategoryIsRejectedWithoutSaving()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = customer.Id,
            Status = FeedbackStatus.Submitted
        };
        var existingCategory = new FeedbackCategoryEntity { Id = Guid.NewGuid(), IsActive = true };
        var fixture = CreateUpdateFixture(feedback, customer, existingCategory);

        var action = () => fixture.Service.UpdateFeedbackAsync(
            feedback.Id,
            ValidUpdate(Guid.NewGuid()),
            customer.Id);

        await action.Should().ThrowAsync<NotFoundException>();
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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

    private static UpdateFeedbackRequest ValidUpdate(Guid categoryId) => new()
    {
        Title = "Updated title",
        Description = "Updated description",
        CategoryId = categoryId,
        Rating = 4
    };

    private static UpdateFixture CreateUpdateFixture(
        Domain.Entities.Feedback feedback,
        User customer,
        FeedbackCategoryEntity category)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var feedbacks = new Mock<IFeedbackRepository>();
        var users = new Mock<IUserRepository>();
        var categories = new Mock<IFeedbackCategoryRepository>();
        var mapper = new Mock<IMapper>();

        unitOfWork.SetupGet(unit => unit.Feedbacks).Returns(feedbacks.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.FeedbackCategories).Returns(categories.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        users.Setup(repository => repository.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        categories.Setup(repository => repository.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        mapper.Setup(value => value.Map<FeedbackDetailDto>(It.IsAny<Domain.Entities.Feedback>()))
            .Returns((Domain.Entities.Feedback value) => new FeedbackDetailDto
            {
                Id = value.Id,
                Title = value.Title,
                Description = value.Description,
                CategoryId = value.CategoryId,
                Rating = value.Rating,
                Status = value.Status,
                SubmittedByUserId = value.SubmittedByUserId
            });

        var service = new FeedbackService(
            unitOfWork.Object,
            mapper.Object,
            Mock.Of<INotificationService>(),
            Mock.Of<IAuditLogService>(),
            Mock.Of<ISupabaseStorageService>());

        return new UpdateFixture(service, unitOfWork);
    }

    private sealed record UpdateFixture(FeedbackService Service, Mock<IUnitOfWork> UnitOfWork);

}
