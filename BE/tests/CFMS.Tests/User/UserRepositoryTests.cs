using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Infrastructure.Persistence;
using CFMS.Infrastructure.Repositories.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CFMS.Tests.Users;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetByRoleAsync_ForSupportStaff_ReturnsOnlyActiveStaffCandidates()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"active-staff-candidates-{Guid.NewGuid()}")
            .Options;
        await using var context = new AppDbContext(options);
        var activeStaff = CreateUser("active.staff@example.com", UserRole.SupportStaff, UserStatus.Active);
        var inactiveStaff = CreateUser("inactive.staff@example.com", UserRole.SupportStaff, UserStatus.Disabled);
        var activeCustomer = CreateUser("active.customer@example.com", UserRole.Customer, UserStatus.Active);
        context.Users.AddRange(activeStaff, inactiveStaff, activeCustomer);
        await context.SaveChangesAsync();

        var candidates = await new UserRepository(context).GetByRoleAsync(UserRole.SupportStaff);

        candidates.Should().ContainSingle(user => user.Id == activeStaff.Id);
        candidates.Should().NotContain(user => user.Id == inactiveStaff.Id);
        candidates.Should().OnlyContain(user =>
            user.Role == UserRole.SupportStaff && user.Status == UserStatus.Active);
    }

    private static User CreateUser(string email, UserRole role, UserStatus status) => new()
    {
        Email = email,
        FirstName = "Test",
        LastName = "User",
        Role = role,
        Status = status
    };
}
