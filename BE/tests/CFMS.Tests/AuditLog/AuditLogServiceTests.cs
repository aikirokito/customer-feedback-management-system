using AutoMapper;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.Services.Implementations;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.AuditLogs;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_PersistsCompleteAuditEntry()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IAuditLogRepository>();
        AuditLog? saved = null;
        unitOfWork.SetupGet(x => x.AuditLogs).Returns(repository.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repository.Setup(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((entry, _) => saved = entry)
            .Returns(Task.CompletedTask);
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        await new AuditLogService(unitOfWork.Object, Mock.Of<IMapper>())
            .LogAsync(userId, AuditAction.Update, "Feedback", entityId, "old", "new", "127.0.0.1");

        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(userId);
        saved.EntityId.Should().Be(entityId);
        saved.OldValues.Should().Be("old");
        saved.NewValues.Should().Be("new");
        saved.IpAddress.Should().Be("127.0.0.1");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
