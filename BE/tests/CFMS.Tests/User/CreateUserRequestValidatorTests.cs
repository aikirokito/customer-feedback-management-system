using CFMS.Application.DTOs.Users;
using CFMS.Application.Validators.Users;
using CFMS.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CFMS.Tests.Users;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void Validate_MissingRequiredFields_IsRejected()
    {
        var result = _validator.Validate(new CreateUserRequest
        {
            Email = string.Empty,
            FirstName = "   ",
            LastName = string.Empty,
            Password = string.Empty,
            ConfirmPassword = string.Empty,
            Role = UserRole.SupportStaff
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().Contain(new[]
        {
            nameof(CreateUserRequest.Email),
            nameof(CreateUserRequest.FirstName),
            nameof(CreateUserRequest.LastName),
            nameof(CreateUserRequest.Password),
            nameof(CreateUserRequest.ConfirmPassword)
        });
    }

    [Fact]
    public void Validate_InvalidEmail_IsRejected()
    {
        var request = ValidRequest();
        request.Email = "not-an-email";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.SystemAdmin)]
    public void Validate_UnsupportedRole_IsRejected(UserRole role)
    {
        var request = ValidRequest();
        request.Role = role;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateUserRequest.Role));
    }

    [Theory]
    [InlineData(UserRole.SupportStaff)]
    [InlineData(UserRole.DepartmentManager)]
    public void Validate_StaffAndManager_AreAccepted(UserRole role)
    {
        var request = ValidRequest();
        request.Role = role;

        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    private static CreateUserRequest ValidRequest() => new()
    {
        Email = "new.user@example.com",
        Password = "Password1!",
        ConfirmPassword = "Password1!",
        FirstName = "New",
        LastName = "User",
        Role = UserRole.SupportStaff
    };
}
