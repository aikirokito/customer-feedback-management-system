using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Users;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IFeedbackRepository> _feedbacks = new();
    private readonly Mock<IAuditLogService> _auditLogs = new();

    public UserServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(x => x.RefreshTokens).Returns(_refreshTokens.Object);
        _unitOfWork.SetupGet(x => x.Feedbacks).Returns(_feedbacks.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _refreshTokens.Setup(x => x.RevokeAllUserTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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
    }

    [Fact]
    public async Task DeleteUser_WhenActorDeletesSelf_IsRejected()
    {
        var admin = new CFMS.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Role = UserRole.SystemAdmin
        };
        _users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var action = () => CreateService().DeleteUserAsync(admin.Id, admin.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*cannot delete their own account*");
        _refreshTokens.Verify(x => x.RevokeAllUserTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_RevokesTokensMarksActorAndWritesAuditLog()
    {
        var actorId = Guid.NewGuid();
        var target = new CFMS.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "target@example.com",
            Role = UserRole.Customer
        };
        _users.Setup(x => x.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        await CreateService().DeleteUserAsync(target.Id, actorId);

        target.IsDeleted.Should().BeTrue();
        target.DeletedAtUtc.Should().NotBeNull();
        target.DeletedByUserId.Should().Be(actorId);
        _refreshTokens.Verify(x => x.RevokeAllUserTokensAsync(target.Id, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogs.Verify(x => x.LogAsync(
            actorId,
            AuditAction.Delete,
            nameof(CFMS.Domain.Entities.User),
            target.Id,
            null,
            It.Is<string>(value => value.Contains(target.Email)),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateSupportStaff_WithActiveFeedback_IsRejected()
    {
        var target = new CFMS.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.SupportStaff,
            Status = UserStatus.Active
        };
        _users.Setup(repository => repository.GetByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        _feedbacks.Setup(repository => repository.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<CFMS.Domain.Entities.Feedback, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = () => CreateService().DeactivateUserAsync(target.Id, Guid.NewGuid());

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Reassign or unassign*");
        target.Status.Should().Be(UserStatus.Active);
        _refreshTokens.Verify(repository => repository.RevokeAllUserTokensAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private UserService CreateService()
        => new(_unitOfWork.Object, Mock.Of<IMapper>(), _auditLogs.Object);
}
