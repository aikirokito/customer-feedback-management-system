using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Assignments;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.DTOs.Responses;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Assignment;

public class FeedbackAssignmentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IFeedbackRepository> _feedbacks = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IAuditLogService> _auditLogs = new();

    public FeedbackAssignmentServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Feedbacks).Returns(_feedbacks.Object);
        _unitOfWork.SetupGet(x => x.Users).Returns(_users.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _notifications.Setup(x => x.SendNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _auditLogs.Setup(x => x.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mapper.Setup(x => x.Map<AssignmentDto>(It.IsAny<object>()))
            .Returns((object source) => new AssignmentDto { Id = ((FeedbackAssignment)source).Id });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AssignFeedback_ManagerCanAssignOrReassignAcrossDepartments(bool isReassignment)
    {
        var feedbackDepartmentId = Guid.NewGuid();
        var staffDepartmentId = Guid.NewGuid();
        var manager = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.DepartmentManager,
            Status = UserStatus.Active,
            DepartmentId = Guid.NewGuid()
        };
        var assignee = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active,
            DepartmentId = staffDepartmentId
        };
        var previousAssignment = new FeedbackAssignment
        {
            AssignedToUserId = Guid.NewGuid(),
            AssignedByUserId = manager.Id,
            IsActive = true
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Cross-department assignment",
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = isReassignment ? previousAssignment.AssignedToUserId : null,
            DepartmentId = feedbackDepartmentId,
            Status = isReassignment ? FeedbackStatus.Assigned : FeedbackStatus.Submitted,
            AssignmentHistory = isReassignment
                ? new List<FeedbackAssignment> { previousAssignment }
                : new List<FeedbackAssignment>()
        };
        _feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(x => x.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>())).ReturnsAsync(manager);
        _users.Setup(x => x.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignee);

        await CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = assignee.Id
        }, manager.Id);

        feedback.AssignedToUserId.Should().Be(assignee.Id);
        feedback.AssignmentHistory.Should().ContainSingle(assignment =>
            assignment.IsActive && assignment.AssignedToUserId == assignee.Id);
        feedback.AssignmentHistory.Should().HaveCount(isReassignment ? 2 : 1);
        feedback.StatusHistory.Should().HaveCount(isReassignment ? 0 : 1);
        _feedbacks.Verify(x => x.AddAssignmentAsync(
            It.Is<FeedbackAssignment>(assignment => assignment.AssignedToUserId == assignee.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _feedbacks.Verify(x => x.AddStatusHistoryAsync(
            It.IsAny<FeedbackStatusHistory>(),
            It.IsAny<CancellationToken>()), isReassignment ? Times.Never() : Times.Once());
        if (isReassignment)
        {
            previousAssignment.IsActive.Should().BeFalse();
        }
    }

    [Fact]
    public async Task ReassignFeedback_WhenAssigned_ChangesActiveStaffWithoutChangingStatusOrHistory()
    {
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var previousStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var newStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var previousAssignment = new FeedbackAssignment
        {
            AssignedToUserId = previousStaff.Id,
            AssignedByUserId = manager.Id,
            IsActive = true
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Assigned feedback",
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = previousStaff.Id,
            Status = FeedbackStatus.Assigned,
            AssignmentHistory = new List<FeedbackAssignment> { previousAssignment }
        };
        SetupAssignmentActors(feedback, manager, previousStaff, newStaff);

        await CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = newStaff.Id
        }, manager.Id);

        feedback.Status.Should().Be(FeedbackStatus.Assigned);
        feedback.AssignedToUserId.Should().Be(newStaff.Id);
        previousAssignment.IsActive.Should().BeFalse();
        feedback.AssignmentHistory.Should().ContainSingle(assignment =>
            assignment.IsActive && assignment.AssignedToUserId == newStaff.Id);
        feedback.StatusHistory.Should().BeEmpty();
        _feedbacks.Verify(repository => repository.AddStatusHistoryAsync(
            It.IsAny<FeedbackStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReassignFeedback_WhenInProgress_ResetsToAssignedAndPreservesSubmissionFields()
    {
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var previousStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var newStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var previousAssignment = new FeedbackAssignment
        {
            AssignedToUserId = previousStaff.Id,
            AssignedByUserId = manager.Id,
            IsActive = true
        };
        var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);
        var ownerId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Keep this title",
            Description = "Keep this description",
            SubmittedByUserId = ownerId,
            CategoryId = categoryId,
            Rating = 4,
            Priority = FeedbackPriority.High,
            AssignedToUserId = previousStaff.Id,
            Status = FeedbackStatus.InProgress,
            UpdatedAtUtc = originalUpdatedAt,
            AssignmentHistory = new List<FeedbackAssignment> { previousAssignment }
        };
        SetupAssignmentActors(feedback, manager, previousStaff, newStaff);

        await CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = newStaff.Id
        }, manager.Id);

        feedback.Status.Should().Be(FeedbackStatus.Assigned);
        feedback.AssignedToUserId.Should().Be(newStaff.Id);
        previousAssignment.IsActive.Should().BeFalse();
        feedback.AssignmentHistory.Should().ContainSingle(assignment =>
            assignment.IsActive && assignment.AssignedToUserId == newStaff.Id);
        feedback.StatusHistory.Should().ContainSingle(history =>
            history.FromStatus == FeedbackStatus.InProgress &&
            history.ToStatus == FeedbackStatus.Assigned &&
            history.ChangedByUserId == manager.Id);
        _feedbacks.Verify(repository => repository.AddStatusHistoryAsync(
            It.Is<FeedbackStatusHistory>(history =>
                history.FromStatus == FeedbackStatus.InProgress &&
                history.ToStatus == FeedbackStatus.Assigned),
            It.IsAny<CancellationToken>()), Times.Once);
        feedback.UpdatedAtUtc.Should().BeAfter(originalUpdatedAt);
        feedback.Title.Should().Be("Keep this title");
        feedback.Description.Should().Be("Keep this description");
        feedback.SubmittedByUserId.Should().Be(ownerId);
        feedback.CategoryId.Should().Be(categoryId);
        feedback.Rating.Should().Be(4);
        feedback.Priority.Should().Be(FeedbackPriority.High);
    }

    [Fact]
    public async Task ReassignFeedback_PreviousStaffCannotProcessOrRespondAndNewStaffCanProceed()
    {
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var previousStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var newStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Authorization follows reassignment",
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = previousStaff.Id,
            Status = FeedbackStatus.InProgress,
            AssignmentHistory = new List<FeedbackAssignment>
            {
                new()
                {
                    AssignedToUserId = previousStaff.Id,
                    AssignedByUserId = manager.Id,
                    IsActive = true
                }
            }
        };
        SetupAssignmentActors(feedback, manager, previousStaff, newStaff);
        await CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = newStaff.Id
        }, manager.Id);
        var workflowService = new FeedbackService(
            _unitOfWork.Object,
            _mapper.Object,
            _notifications.Object,
            _auditLogs.Object,
            Mock.Of<ISupabaseStorageService>());
        var responseService = new FeedbackResponseService(
            _unitOfWork.Object,
            _mapper.Object,
            _notifications.Object,
            _auditLogs.Object);

        var previousStaffAction = () => workflowService.ChangeStatusAsync(feedback.Id, new ChangeFeedbackStatusRequest
        {
            NewStatus = FeedbackStatus.InProgress
        }, previousStaff.Id);

        await previousStaffAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*assigned feedback*");
        var previousStaffResponseAction = () => responseService.CreateResponseAsync(new CreateResponseRequest
        {
            FeedbackId = feedback.Id,
            Content = "This response must be rejected."
        }, previousStaff.Id);
        await previousStaffResponseAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*assigned feedback*");
        feedback.Status.Should().Be(FeedbackStatus.Assigned);

        await workflowService.ChangeStatusAsync(feedback.Id, new ChangeFeedbackStatusRequest
        {
            NewStatus = FeedbackStatus.InProgress
        }, newStaff.Id);

        feedback.Status.Should().Be(FeedbackStatus.InProgress);
        feedback.StatusHistory.Should().ContainSingle(history =>
            history.FromStatus == FeedbackStatus.Assigned &&
            history.ToStatus == FeedbackStatus.InProgress &&
            history.ChangedByUserId == newStaff.Id);

        await responseService.CreateResponseAsync(new CreateResponseRequest
        {
            FeedbackId = feedback.Id,
            Content = "The newly assigned Staff can respond."
        }, newStaff.Id);

        feedback.Responses.Should().ContainSingle(response => response.RespondedByUserId == newStaff.Id);
    }

    [Fact]
    public async Task ReassignFeedback_InvalidAssigneeDoesNotPartiallyChangeAggregateOrSave()
    {
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var previousStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var invalidAssignee = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var previousAssignment = new FeedbackAssignment
        {
            AssignedToUserId = previousStaff.Id,
            AssignedByUserId = manager.Id,
            IsActive = true
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = previousStaff.Id,
            Status = FeedbackStatus.InProgress,
            AssignmentHistory = new List<FeedbackAssignment> { previousAssignment }
        };
        SetupAssignmentActors(feedback, manager, previousStaff, invalidAssignee);

        var action = () => CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = invalidAssignee.Id
        }, manager.Id);

        await action.Should().ThrowAsync<BusinessRuleException>();
        feedback.AssignedToUserId.Should().Be(previousStaff.Id);
        feedback.Status.Should().Be(FeedbackStatus.InProgress);
        previousAssignment.IsActive.Should().BeTrue();
        feedback.AssignmentHistory.Should().ContainSingle();
        feedback.StatusHistory.Should().BeEmpty();
        _feedbacks.Verify(repository => repository.AddAssignmentAsync(
            It.IsAny<FeedbackAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        _feedbacks.Verify(repository => repository.AddStatusHistoryAsync(
            It.IsAny<FeedbackStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReassignFeedback_DisabledStaffIsRejectedWithoutPartialChanges()
    {
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var previousStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var inactiveStaff = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Disabled };
        var previousAssignment = new FeedbackAssignment
        {
            AssignedToUserId = previousStaff.Id,
            AssignedByUserId = manager.Id,
            IsActive = true
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = previousStaff.Id,
            Status = FeedbackStatus.InProgress,
            AssignmentHistory = new List<FeedbackAssignment> { previousAssignment }
        };
        SetupAssignmentActors(feedback, manager, previousStaff, inactiveStaff);

        var action = () => CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = inactiveStaff.Id
        }, manager.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*disabled staff account*");
        feedback.AssignedToUserId.Should().Be(previousStaff.Id);
        feedback.Status.Should().Be(FeedbackStatus.InProgress);
        previousAssignment.IsActive.Should().BeTrue();
        feedback.AssignmentHistory.Should().ContainSingle();
        feedback.StatusHistory.Should().BeEmpty();
        _feedbacks.Verify(repository => repository.AddAssignmentAsync(
            It.IsAny<FeedbackAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        _feedbacks.Verify(repository => repository.AddStatusHistoryAsync(
            It.IsAny<FeedbackStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignFeedback_ActiveStaffWithoutDepartment_IsAllowed()
    {
        var manager = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.DepartmentManager,
            Status = UserStatus.Active
        };
        var assignee = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active,
            DepartmentId = null
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Assignment without department",
            SubmittedByUserId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = FeedbackStatus.Submitted
        };
        _feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(x => x.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>())).ReturnsAsync(manager);
        _users.Setup(x => x.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignee);

        await CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = assignee.Id
        }, manager.Id);

        feedback.AssignedToUserId.Should().Be(assignee.Id);
        feedback.AssignmentHistory.Should().ContainSingle(assignment =>
            assignment.IsActive && assignment.AssignedToUserId == assignee.Id);
    }

    [Theory]
    [InlineData(UserRole.SupportStaff, UserStatus.Disabled, "disabled staff account")]
    [InlineData(UserRole.Customer, UserStatus.Active, "active Support Staff")]
    public async Task AssignFeedback_IneligibleUser_IsRejected(
        UserRole assigneeRole,
        UserStatus assigneeStatus,
        string expectedMessage)
    {
        var manager = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.DepartmentManager,
            Status = UserStatus.Active
        };
        var assignee = new User
        {
            Id = Guid.NewGuid(),
            Role = assigneeRole,
            Status = assigneeStatus
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Invalid assignee",
            SubmittedByUserId = Guid.NewGuid(),
            Status = FeedbackStatus.Submitted
        };
        _feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(x => x.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>())).ReturnsAsync(manager);
        _users.Setup(x => x.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignee);

        var action = () => CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = assignee.Id
        }, manager.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage($"*{expectedMessage}*");
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignFeedback_SystemAdminIsRejectedWithoutSaving()
    {
        var admin = new User { Id = Guid.NewGuid(), Role = UserRole.SystemAdmin, Status = UserStatus.Active };
        var assignee = new User { Id = Guid.NewGuid(), Role = UserRole.SupportStaff, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            Status = FeedbackStatus.Submitted
        };
        _feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(
                feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(repository => repository.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        _users.Setup(repository => repository.GetByIdAsync(assignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);

        var action = () => CreateService().AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = assignee.Id
        }, admin.Id);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Only Department Managers*");
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnassignFeedback_ResetsWorkflowAndAllowsAssignmentAgain()
    {
        var departmentId = Guid.NewGuid();
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var previousStaff = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active,
            DepartmentId = departmentId
        };
        var newStaff = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active,
            DepartmentId = departmentId
        };
        var activeAssignment = new FeedbackAssignment
        {
            AssignedToUserId = previousStaff.Id,
            AssignedByUserId = manager.Id,
            IsActive = true
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            Title = "Reassign after triage",
            SubmittedByUserId = Guid.NewGuid(),
            AssignedToUserId = previousStaff.Id,
            DepartmentId = departmentId,
            Status = FeedbackStatus.InProgress,
            AssignmentHistory = new List<FeedbackAssignment> { activeAssignment }
        };

        _feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(x => x.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>())).ReturnsAsync(manager);
        _users.Setup(x => x.GetByIdAsync(newStaff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newStaff);

        var service = CreateService();
        await service.UnassignFeedbackAsync(feedback.Id, manager.Id);

        feedback.AssignedToUserId.Should().BeNull();
        feedback.Status.Should().Be(FeedbackStatus.Submitted);
        activeAssignment.IsActive.Should().BeFalse();
        feedback.StatusHistory.Should().ContainSingle(entry =>
            entry.FromStatus == FeedbackStatus.InProgress &&
            entry.ToStatus == FeedbackStatus.Submitted &&
            entry.ChangedByUserId == manager.Id);

        await service.AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = newStaff.Id
        }, manager.Id);

        feedback.AssignedToUserId.Should().Be(newStaff.Id);
        feedback.Status.Should().Be(FeedbackStatus.Assigned);
        feedback.AssignmentHistory.Should().ContainSingle(entry => entry.IsActive && entry.AssignedToUserId == newStaff.Id);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UnassignFeedback_WhenNotAssigned_IsRejected()
    {
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            Status = FeedbackStatus.Submitted
        };
        _feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(x => x.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>())).ReturnsAsync(manager);

        var action = () => CreateService().UnassignFeedbackAsync(feedback.Id, manager.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*not currently assigned*");
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAssignmentHistory_CustomerIsRejected()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = customer.Id,
            Status = FeedbackStatus.Submitted
        };
        _feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(repository => repository.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var action = () => CreateService().GetAssignmentHistoryAsync(feedback.Id, customer.Id);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*internal assignment history*");
    }

    [Fact]
    public async Task GetAssignmentHistory_WhenAssignedStaffIsNowInactive_RemainsReadable()
    {
        var manager = new User { Id = Guid.NewGuid(), Role = UserRole.DepartmentManager, Status = UserStatus.Active };
        var inactiveStaff = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Former",
            LastName = "Staff",
            Role = UserRole.SupportStaff,
            Status = UserStatus.Disabled
        };
        var historicalAssignment = new FeedbackAssignment
        {
            Id = Guid.NewGuid(),
            AssignedToUserId = inactiveStaff.Id,
            AssignedToUser = inactiveStaff,
            AssignedByUserId = manager.Id,
            AssignedByUser = manager,
            IsActive = false
        };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            Status = FeedbackStatus.Resolved,
            AssignmentHistory = new List<FeedbackAssignment> { historicalAssignment }
        };
        historicalAssignment.FeedbackId = feedback.Id;
        _feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(
                feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(repository => repository.GetByIdAsync(
                manager.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);
        _mapper.Setup(mapper => mapper.Map<IEnumerable<AssignmentDto>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<FeedbackAssignment>)source)
                .Select(assignment => new AssignmentDto
                {
                    Id = assignment.Id,
                    FeedbackId = assignment.FeedbackId,
                    AssignedToUserName = assignment.AssignedToUser.FullName,
                    IsActive = assignment.IsActive
                })
                .ToList());

        var history = await CreateService().GetAssignmentHistoryAsync(feedback.Id, manager.Id);

        history.Should().ContainSingle(item =>
            item.Id == historicalAssignment.Id &&
            item.AssignedToUserName == "Former Staff" &&
            !item.IsActive);
    }

    private void SetupAssignmentActors(
        Domain.Entities.Feedback feedback,
        User manager,
        User previousStaff,
        User targetAssignee)
    {
        _feedbacks.Setup(repository => repository.GetByIdWithDetailsAsync(
                feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(repository => repository.GetByIdAsync(
                manager.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);
        _users.Setup(repository => repository.GetByIdAsync(
                previousStaff.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousStaff);
        _users.Setup(repository => repository.GetByIdAsync(
                targetAssignee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAssignee);
    }

    private FeedbackAssignmentService CreateService()
        => new(
            _unitOfWork.Object,
            _mapper.Object,
            _notifications.Object,
            _auditLogs.Object);
}
