using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Categories;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using CFMS.Application.Validators.Categories;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Infrastructure.Persistence;
using CFMS.Infrastructure.Repositories.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CFMS.Tests.Category;

public class FeedbackCategoryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IFeedbackCategoryRepository> _categories = new();
    private readonly Mock<IRepository<Department>> _departments = new();
    private readonly Mock<IAuditLogService> _auditLogs = new();
    private readonly Mock<IMapper> _mapper = new();

    public FeedbackCategoryServiceTests()
    {
        _unitOfWork.SetupGet(x => x.FeedbackCategories).Returns(_categories.Object);
        _unitOfWork.SetupGet(x => x.Departments).Returns(_departments.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<CategoryDto>(It.IsAny<object>()))
            .Returns((object source) =>
            {
                var category = (FeedbackCategoryEntity)source;
                return new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    DepartmentId = category.DepartmentId,
                    DepartmentName = category.Department?.Name,
                    CreatedAtUtc = category.CreatedAtUtc
                };
            });
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesActiveCategoryAndAuditLog()
    {
        var actorId = Guid.NewGuid();
        var department = new Department { Id = Guid.NewGuid(), Name = "Customer Care", IsActive = true };
        FeedbackCategoryEntity? savedCategory = null;
        _categories.Setup(x => x.GetByNameAsync("Complaint", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedbackCategoryEntity?)null);
        _departments.Setup(x => x.GetByIdAsync(department.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);
        _categories.Setup(x => x.AddAsync(It.IsAny<FeedbackCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FeedbackCategoryEntity, CancellationToken>((category, _) => savedCategory = category)
            .Returns(Task.CompletedTask);

        var result = await CreateService().CreateAsync(new CreateCategoryRequest
        {
            Name = "  Complaint  ",
            Description = "  Customer complaints  ",
            DepartmentId = department.Id
        }, actorId);

        savedCategory.Should().NotBeNull();
        savedCategory!.Name.Should().Be("Complaint");
        savedCategory.Description.Should().Be("Customer complaints");
        savedCategory.IsActive.Should().BeTrue();
        savedCategory.Department.Should().BeSameAs(department);
        result.DepartmentName.Should().Be("Customer Care");
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditLogs.Verify(x => x.LogAsync(
            actorId,
            AuditAction.Create,
            nameof(FeedbackCategoryEntity),
            savedCategory.Id,
            null,
            It.Is<string>(value => value.Contains("Name=Complaint")),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsConflictWithoutWriting()
    {
        _categories.Setup(x => x.GetByNameAsync("Complaint", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackCategoryEntity { Name = "Complaint" });

        var action = () => CreateService().CreateAsync(
            new CreateCategoryRequest { Name = "Complaint" },
            Guid.NewGuid());

        await action.Should().ThrowAsync<ConflictException>();
        _categories.Verify(x => x.AddAsync(It.IsAny<FeedbackCategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithDisabledDepartment_ThrowsBusinessRuleException()
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Legacy", IsActive = false };
        _categories.Setup(x => x.GetByNameAsync("Legacy issues", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedbackCategoryEntity?)null);
        _departments.Setup(x => x.GetByIdAsync(department.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        var action = () => CreateService().CreateAsync(new CreateCategoryRequest
        {
            Name = "Legacy issues",
            DepartmentId = department.Id
        }, Guid.NewGuid());

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Disabled departments*");
    }

    [Fact]
    public async Task UpdateAsync_DisablesCategoryAndPreservesExistingFeedbackReference()
    {
        var feedback = new Domain.Entities.Feedback { Id = Guid.NewGuid(), Title = "Existing feedback" };
        var category = new FeedbackCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Service",
            IsActive = true,
            Feedbacks = new List<Domain.Entities.Feedback> { feedback }
        };
        feedback.Category = category;
        feedback.CategoryId = category.Id;
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        await CreateService().UpdateAsync(
            category.Id,
            new UpdateCategoryRequest { IsActive = false },
            Guid.NewGuid());

        category.IsActive.Should().BeFalse();
        category.Feedbacks.Should().ContainSingle().Which.Should().BeSameAs(feedback);
        feedback.CategoryId.Should().Be(category.Id);
        _categories.Verify(x => x.Remove(It.IsAny<FeedbackCategoryEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CanClearDepartmentAssignment()
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Support", IsActive = true };
        var category = new FeedbackCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Product",
            DepartmentId = department.Id,
            Department = department
        };
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var result = await CreateService().UpdateAsync(
            category.Id,
            new UpdateCategoryRequest { ClearDepartment = true },
            Guid.NewGuid());

        category.DepartmentId.Should().BeNull();
        category.Department.Should().BeNull();
        result.DepartmentName.Should().BeNull();
    }

    [Fact]
    public void UpdateValidator_RejectsEmptyOrConflictingRequests()
    {
        var validator = new UpdateCategoryRequestValidator();

        validator.Validate(new UpdateCategoryRequest()).IsValid.Should().BeFalse();
        validator.Validate(new UpdateCategoryRequest
        {
            DepartmentId = Guid.NewGuid(),
            ClearDepartment = true
        }).IsValid.Should().BeFalse();
    }

    private FeedbackCategoryService CreateService()
        => new(_unitOfWork.Object, _mapper.Object, _auditLogs.Object);
}

public class FeedbackCategorySubmissionTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IFeedbackCategoryRepository> _categories = new();
    private readonly Mock<IFeedbackRepository> _feedbacks = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IAuditLogService> _auditLogs = new();
    private readonly Mock<ISupabaseStorageService> _storage = new();

    public FeedbackCategorySubmissionTests()
    {
        _unitOfWork.SetupGet(x => x.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(x => x.FeedbackCategories).Returns(_categories.Object);
        _unitOfWork.SetupGet(x => x.Feedbacks).Returns(_feedbacks.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateFeedback_WithDisabledCategory_IsRejected()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var category = new FeedbackCategoryEntity { Id = Guid.NewGuid(), Name = "Disabled", IsActive = false };
        _users.Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var action = () => CreateService().CreateFeedbackAsync(new CreateFeedbackRequest
        {
            Title = "Test",
            Description = "Test feedback",
            CategoryId = category.Id
        }, customer.Id);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*category is disabled*");
        _feedbacks.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Feedback>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFeedback_WithActiveCategory_StoresCategoryAndDefaultWorkflowValues()
    {
        var customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, Status = UserStatus.Active };
        var category = new FeedbackCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Service",
            IsActive = true,
            DepartmentId = Guid.NewGuid()
        };
        Domain.Entities.Feedback? savedFeedback = null;
        _users.Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _feedbacks.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Feedback>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Feedback, CancellationToken>((feedback, _) => savedFeedback = feedback)
            .Returns(Task.CompletedTask);
        _feedbacks.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedFeedback);
        _mapper.Setup(x => x.Map<FeedbackDetailDto>(It.IsAny<object>()))
            .Returns((object source) => new FeedbackDetailDto { Id = ((Domain.Entities.Feedback)source).Id });

        await CreateService().CreateFeedbackAsync(new CreateFeedbackRequest
        {
            Title = "Service feedback",
            Description = "A detailed customer issue",
            CategoryId = category.Id
        }, customer.Id);

        savedFeedback.Should().NotBeNull();
        savedFeedback!.CategoryId.Should().Be(category.Id);
        savedFeedback.Category.Should().BeSameAs(category);
        savedFeedback.DepartmentId.Should().Be(category.DepartmentId);
        savedFeedback.Status.Should().Be(FeedbackStatus.New);
        savedFeedback.Priority.Should().Be(FeedbackPriority.Medium);
        savedFeedback.StatusHistory.Should().ContainSingle(x =>
            x.FromStatus == FeedbackStatus.New && x.ToStatus == FeedbackStatus.New);
    }

    private FeedbackService CreateService()
        => new(
            _unitOfWork.Object,
            _mapper.Object,
            _notifications.Object,
            _auditLogs.Object,
            _storage.Object);
}

public class FeedbackCategoryRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly FeedbackCategoryRepository _repository;

    public FeedbackCategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"category-tests-{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(options);
        _repository = new FeedbackCategoryRepository(_context);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveCategoriesInNameOrder()
    {
        _context.FeedbackCategories.AddRange(
            new FeedbackCategoryEntity { Name = "Website", IsActive = true },
            new FeedbackCategoryEntity { Name = "Complaint", IsActive = true },
            new FeedbackCategoryEntity { Name = "Disabled", IsActive = false });
        await _context.SaveChangesAsync();

        var result = (await _repository.GetActiveAsync()).ToList();

        result.Select(x => x.Name).Should().Equal("Complaint", "Website");
        result.Should().OnlyContain(x => x.IsActive);
    }

    [Fact]
    public async Task DisablingCategory_DoesNotBreakExistingFeedbackReference()
    {
        var category = new FeedbackCategoryEntity { Name = "Service", IsActive = true };
        var feedback = new Domain.Entities.Feedback
        {
            Title = "Existing service feedback",
            Description = "The original category reference must be preserved.",
            SubmittedByUserId = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category
        };
        _context.FeedbackCategories.Add(category);
        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        category.IsActive = false;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var persistedFeedback = await _context.Feedbacks
            .Include(x => x.Category)
            .SingleAsync(x => x.Id == feedback.Id);

        persistedFeedback.CategoryId.Should().Be(category.Id);
        persistedFeedback.Category.Should().NotBeNull();
        persistedFeedback.Category!.Name.Should().Be("Service");
        persistedFeedback.Category.IsActive.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
