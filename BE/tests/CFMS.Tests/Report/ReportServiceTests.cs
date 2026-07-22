using AutoMapper;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Reports;
using CFMS.Application.Services.Implementations;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using FeedbackEntity = CFMS.Domain.Entities.Feedback;

namespace CFMS.Tests.Report;

public class ReportServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IFeedbackRepository> _feedbacks = new();

    public ReportServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(x => x.Feedbacks).Returns(_feedbacks.Object);
    }

    [Fact]
    public async Task GetFeedbackSummary_CalculatesRequiredSrsMetrics()
    {
        var adminId = Guid.NewGuid();
        _users.Setup(x => x.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = adminId, Role = UserRole.SystemAdmin });

        var category = new FeedbackCategoryEntity { Name = "Service" };
        var createdAt = DateTime.UtcNow.AddHours(-10);
        var data = new[]
        {
            new FeedbackEntity { Category = category, Rating = 5, Status = FeedbackStatus.Resolved, CreatedAtUtc = createdAt, ResolvedAtUtc = createdAt.AddHours(2) },
            new FeedbackEntity { Category = category, Rating = 3, Status = FeedbackStatus.Closed, CreatedAtUtc = createdAt, ClosedAtUtc = createdAt.AddHours(6) },
            new FeedbackEntity
            {
                Category = category,
                Status = FeedbackStatus.InProgress,
                Priority = FeedbackPriority.High,
                CreatedAtUtc = createdAt,
                ResolvedAtUtc = createdAt.AddHours(1)
            }
        };

        _feedbacks.Setup(x => x.GetReportFeedbacksAsync(
                null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var service = new ReportService(_unitOfWork.Object, Mock.Of<IMapper>());
        var result = await service.GetFeedbackSummaryAsync(new ReportFilterRequest(), adminId);

        result.TotalFeedbacks.Should().Be(3);
        result.OpenFeedbacks.Should().Be(1);
        result.AverageResolutionTimeHours.Should().Be(4);
        result.AverageRating.Should().Be(4);
        result.ResolutionRate.Should().BeApproximately(66.67, 0.01);
        result.UnresolvedHighPriorityCount.Should().Be(1);
        result.ByCategory["Service"].Should().Be(3);
    }

    [Fact]
    public async Task GetFeedbackSummary_ForManager_DoesNotApplyDepartmentScope()
    {
        var managerId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        _users.Setup(x => x.GetByIdAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = managerId,
                Role = UserRole.DepartmentManager,
                DepartmentId = departmentId
            });

        _feedbacks.Setup(x => x.GetReportFeedbacksAsync(
                null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FeedbackEntity>());

        var service = new ReportService(_unitOfWork.Object, Mock.Of<IMapper>());
        await service.GetFeedbackSummaryAsync(new ReportFilterRequest(), managerId);

        _feedbacks.Verify(x => x.GetReportFeedbacksAsync(
            null, null, null, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFeedbackTrend_GroupsByMonthInChronologicalOrder()
    {
        var adminId = Guid.NewGuid();
        _users.Setup(x => x.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = adminId, Role = UserRole.SystemAdmin });
        _feedbacks.Setup(x => x.GetReportFeedbacksAsync(null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new FeedbackEntity { CreatedAtUtc = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), Status = FeedbackStatus.Closed },
                new FeedbackEntity { CreatedAtUtc = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), Status = FeedbackStatus.Resolved },
                new FeedbackEntity { CreatedAtUtc = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), Status = FeedbackStatus.InProgress }
            });

        var result = (await new ReportService(_unitOfWork.Object, Mock.Of<IMapper>())
            .GetFeedbackTrendAsync(new ReportFilterRequest(), adminId)).ToList();

        result.Select(point => point.Period).Should().Equal("2026-01", "2026-02");
        result[0].ResolvedCount.Should().Be(1);
        result[1].TotalCount.Should().Be(2);
        result[1].ClosedCount.Should().Be(1);
    }
}
