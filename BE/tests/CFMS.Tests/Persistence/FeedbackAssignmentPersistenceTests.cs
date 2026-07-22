using AutoMapper;
using CFMS.Application.DTOs.Assignments;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using EfUnitOfWork = CFMS.Infrastructure.UnitOfWork.UnitOfWork;

namespace CFMS.Tests.Persistence;

public class FeedbackAssignmentPersistenceTests
{
    [Fact]
    public async Task AssignThenReassign_PersistsOneHistoryRowPerAssignmentWithoutDuplicates()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"assignment-persistence-{Guid.NewGuid()}")
            .Options;
        var context = new AppDbContext(options);
        var manager = CreateUser("manager@example.com", UserRole.DepartmentManager);
        var firstStaff = CreateUser("first.staff@example.com", UserRole.SupportStaff);
        var secondStaff = CreateUser("second.staff@example.com", UserRole.SupportStaff);
        var customer = CreateUser("customer@example.com", UserRole.Customer);
        var category = new FeedbackCategoryEntity { Name = "Service", IsActive = true };
        var feedback = new Domain.Entities.Feedback
        {
            Title = "Assignment persistence",
            Description = "Verify EF state semantics",
            CategoryId = category.Id,
            Category = category,
            SubmittedByUserId = customer.Id,
            SubmittedByUser = customer,
            Status = FeedbackStatus.Submitted
        };
        context.AddRange(manager, firstStaff, secondStaff, customer, category, feedback);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        using var unitOfWork = new EfUnitOfWork(context);
        var service = CreateService(unitOfWork);

        await service.AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = firstStaff.Id
        }, manager.Id);

        context.ChangeTracker.Clear();
        var initialAssignments = await context.FeedbackAssignments
            .Where(assignment => assignment.FeedbackId == feedback.Id)
            .ToListAsync();
        var initialStatusHistory = await context.FeedbackStatusHistories
            .Where(history => history.FeedbackId == feedback.Id)
            .ToListAsync();

        initialAssignments.Should().ContainSingle(assignment =>
            assignment.IsActive && assignment.AssignedToUserId == firstStaff.Id);
        initialStatusHistory.Should().ContainSingle(history =>
            history.FromStatus == FeedbackStatus.Submitted &&
            history.ToStatus == FeedbackStatus.Assigned);

        context.ChangeTracker.Clear();
        await service.AssignFeedbackAsync(new AssignFeedbackRequest
        {
            FeedbackId = feedback.Id,
            AssignToUserId = secondStaff.Id
        }, manager.Id);

        context.ChangeTracker.Clear();
        var persistedFeedback = await context.Feedbacks.SingleAsync(item => item.Id == feedback.Id);
        var persistedAssignments = await context.FeedbackAssignments
            .Where(assignment => assignment.FeedbackId == feedback.Id)
            .OrderBy(assignment => assignment.CreatedAtUtc)
            .ToListAsync();
        var persistedStatusHistory = await context.FeedbackStatusHistories
            .Where(history => history.FeedbackId == feedback.Id)
            .ToListAsync();

        persistedFeedback.AssignedToUserId.Should().Be(secondStaff.Id);
        persistedFeedback.Status.Should().Be(FeedbackStatus.Assigned);
        persistedAssignments.Should().HaveCount(2);
        persistedAssignments.Should().ContainSingle(assignment =>
            assignment.IsActive && assignment.AssignedToUserId == secondStaff.Id);
        persistedAssignments.Should().ContainSingle(assignment =>
            !assignment.IsActive && assignment.AssignedToUserId == firstStaff.Id);
        persistedStatusHistory.Should().ContainSingle(history =>
            history.FromStatus == FeedbackStatus.Submitted &&
            history.ToStatus == FeedbackStatus.Assigned);
    }

    private static User CreateUser(string email, UserRole role)
        => new()
        {
            Email = email,
            FirstName = role.ToString(),
            LastName = "User",
            Role = role,
            Status = UserStatus.Active
        };

    private static FeedbackAssignmentService CreateService(EfUnitOfWork unitOfWork)
    {
        var mapper = new Mock<IMapper>();
        mapper.Setup(value => value.Map<AssignmentDto>(It.IsAny<object>()))
            .Returns((object source) => new AssignmentDto { Id = ((FeedbackAssignment)source).Id });
        var notifications = new Mock<INotificationService>();
        notifications.Setup(value => value.SendNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var auditLogs = new Mock<IAuditLogService>();
        auditLogs.Setup(value => value.LogAsync(
                It.IsAny<Guid?>(),
                It.IsAny<AuditAction>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new FeedbackAssignmentService(
            unitOfWork,
            mapper.Object,
            notifications.Object,
            auditLogs.Object);
    }
}
