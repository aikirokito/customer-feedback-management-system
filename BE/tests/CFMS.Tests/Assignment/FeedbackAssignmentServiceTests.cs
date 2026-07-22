using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Assignments;
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
        if (isReassignment)
        {
            previousAssignment.IsActive.Should().BeFalse();
        }
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
    public async Task UnassignFeedback_ResetsWorkflowAndAllowsAssignmentAgain()
    {
        var departmentId = Guid.NewGuid();
        var admin = new User { Id = Guid.NewGuid(), Role = UserRole.SystemAdmin, Status = UserStatus.Active };
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
            AssignedByUserId = admin.Id,
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
        _users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _users.Setup(x => x.GetByIdAsync(newStaff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newStaff);

        var service = CreateService();
        await service.UnassignFeedbackAsync(feedback.Id, admin.Id);

        feedback.AssignedToUserId.Should().BeNull();
        feedback.Status.Should().Be(FeedbackStatus.Submitted);
        activeAssignment.IsActive.Should().BeFalse();
        feedback.StatusHistory.Should().ContainSingle(entry =>
            entry.FromStatus == FeedbackStatus.InProgress &&
            entry.ToStatus == FeedbackStatus.Submitted &&
            entry.ChangedByUserId == admin.Id);

        await service.AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = newStaff.Id
        }, admin.Id);

        feedback.AssignedToUserId.Should().Be(newStaff.Id);
        feedback.Status.Should().Be(FeedbackStatus.Assigned);
        feedback.AssignmentHistory.Should().ContainSingle(entry => entry.IsActive && entry.AssignedToUserId == newStaff.Id);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UnassignFeedback_WhenNotAssigned_IsRejected()
    {
        var admin = new User { Id = Guid.NewGuid(), Role = UserRole.SystemAdmin, Status = UserStatus.Active };
        var feedback = new Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            SubmittedByUserId = Guid.NewGuid(),
            Status = FeedbackStatus.Submitted
        };
        _feedbacks.Setup(x => x.GetByIdWithDetailsAsync(feedback.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var action = () => CreateService().UnassignFeedbackAsync(feedback.Id, admin.Id);

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

    private FeedbackAssignmentService CreateService()
        => new(
            _unitOfWork.Object,
            _mapper.Object,
            _notifications.Object,
            _auditLogs.Object);
}
