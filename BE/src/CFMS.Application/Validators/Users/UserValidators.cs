using CFMS.Application.DTOs.Users;
using CFMS.Domain.Constants;
using CFMS.Domain.Enums;
using FluentValidation;

namespace CFMS.Application.Validators.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(request => request.FirstName)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(request => request.LastName)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(request => request.PhoneNumber)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$")
            .WithMessage("Phone number format is invalid.")
            .When(request => !string.IsNullOrWhiteSpace(request.PhoneNumber));

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(AuthConstants.PasswordMinLength)
                .WithMessage($"Password must be at least {AuthConstants.PasswordMinLength} characters.")
            .MaximumLength(AuthConstants.PasswordMaxLength)
                .WithMessage($"Password must not exceed {AuthConstants.PasswordMaxLength} characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")
                .WithMessage("Password must contain at least one special character.");

        RuleFor(request => request.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(request => request.Password).WithMessage("Passwords do not match.");

        RuleFor(request => request.Role)
            .IsInEnum().WithMessage("Role is invalid.")
            .Must(role => role is UserRole.SupportStaff or UserRole.DepartmentManager)
            .WithMessage("Admin can create only Staff or Manager accounts.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
    }
}

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.DepartmentId)
            .NotEqual(Guid.Empty)
            .When(x => x.DepartmentId.HasValue);
    }
}
