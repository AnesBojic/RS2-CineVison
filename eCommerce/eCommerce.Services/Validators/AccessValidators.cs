using eCommerce.Model.Access;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class UserLoginValidator : AbstractValidator<UserLoginRequest>
    {
        public UserLoginValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");
        }
    }

    public class RefreshAccessTokenValidator : AbstractValidator<RefreshAccessTokenRequest>
    {
        public RefreshAccessTokenValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.")
                .MaximumLength(500).WithMessage("Refresh token cannot exceed 500 characters.");
        }
    }
}
