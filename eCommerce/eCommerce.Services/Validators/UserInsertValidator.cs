using eCommerce.Model;
using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class UserInsertValidator : AbstractValidator<UserInsertRequest>
    {
        public UserInsertValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
                .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .Must(RoleNames.IsKnown)
                .WithMessage($"Role must be {RoleNames.Admin}, {RoleNames.Staff}, or {RoleNames.Customer}.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.ProfileImageBase64)
                .Must(base64 =>
                {
                    if (string.IsNullOrWhiteSpace(base64))
                    {
                        return true;
                    }

                    return ImageContentValidator.TryValidateBase64(base64, out _, out _);
                })
                .WithMessage("Profile image must be a valid JPEG, PNG, GIF, or WebP file.")
                .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageBase64));
        }
    }
}
