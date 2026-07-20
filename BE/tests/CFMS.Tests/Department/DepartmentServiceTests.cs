using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Departments;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Departments;

public class DepartmentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<CFMS.Domain.Entities.Department>> _departments = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IFeedbackCategoryRepository> _categories = new();
    private readonly Mock<IAuditLogService> _auditLogs = new();
    private readonly Mock<IMapper> _mapper = new();

    public DepartmentServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Departments).Returns(_departments.Object);
        _unitOfWork.SetupGet(x => x.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(x => x.FeedbackCategories).Returns(_categories.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _departments.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CFMS.Domain.Entities.Department, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _categories.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<FeedbackCategoryEntity, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _auditLogs.Setup(x => x.LogAsync(It.IsAny<Guid?>(), It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapper.Setup(x => x.Map<DepartmentDto>(It.IsAny<object>())).Returns((object source) =>
        {
            var department = (CFMS.Domain.Entities.Department)source;
            return new DepartmentDto { Id = department.Id, Name = department.Name, Description = department.Description, IsActive = department.IsActive };
        });
    }

    [Fact]
    public async Task Create_TrimsValuesPersistsAndAudits()
    {
        CFMS.Domain.Entities.Department? saved = null;
        _departments.Setup(x => x.AddAsync(It.IsAny<CFMS.Domain.Entities.Department>(), It.IsAny<CancellationToken>()))
            .Callback<CFMS.Domain.Entities.Department, CancellationToken>((department, _) => saved = department)
            .Returns(Task.CompletedTask);
        var actorId = Guid.NewGuid();

        var result = await CreateService().CreateAsync(new CreateDepartmentRequest { Name = " Support ", Description = " Main team " }, actorId);

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Support");
        saved.Description.Should().Be("Main team");
        saved.IsActive.Should().BeTrue();
        result.Name.Should().Be("Support");
        _auditLogs.Verify(x => x.LogAsync(actorId, AuditAction.Create, nameof(CFMS.Domain.Entities.Department), saved.Id, null, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenNameExists_IsRejected()
    {
        _departments.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CFMS.Domain.Entities.Department, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => CreateService().CreateAsync(new CreateDepartmentRequest { Name = "Support" }, Guid.NewGuid());

        await action.Should().ThrowAsync<ConflictException>().WithMessage("*already exists*");
        _departments.Verify(x => x.AddAsync(It.IsAny<CFMS.Domain.Entities.Department>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_CanClearDescriptionAndDisableDepartment()
    {
        var department = new CFMS.Domain.Entities.Department { Id = Guid.NewGuid(), Name = "Support", Description = "Old", IsActive = true };
        _departments.Setup(x => x.GetByIdAsync(department.Id, It.IsAny<CancellationToken>())).ReturnsAsync(department);

        await CreateService().UpdateAsync(department.Id, new UpdateDepartmentRequest { ClearDescription = true, IsActive = false }, Guid.NewGuid());

        department.Description.Should().BeNull();
        department.IsActive.Should().BeFalse();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_DisableWithActiveDependencies_IsRejected()
    {
        var department = new CFMS.Domain.Entities.Department { Id = Guid.NewGuid(), Name = "Support", IsActive = true };
        _departments.Setup(x => x.GetByIdAsync(department.Id, It.IsAny<CancellationToken>())).ReturnsAsync(department);
        _users.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => CreateService().UpdateAsync(department.Id, new UpdateDepartmentRequest { IsActive = false }, Guid.NewGuid());

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*active users and categories*");
        department.IsActive.Should().BeTrue();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private DepartmentService CreateService() => new(_unitOfWork.Object, _mapper.Object, _auditLogs.Object);
}
