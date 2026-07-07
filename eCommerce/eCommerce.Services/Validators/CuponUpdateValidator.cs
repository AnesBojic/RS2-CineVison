using System;
using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class CuponUpdateValidator : AbstractValidator<CuponUpdateRequest>
    {
        public CuponUpdateValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id is required.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Coupon code is required.")
                .MinimumLength(3).WithMessage("Coupon code must be at least 3 characters long.")
                .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.");

            RuleFor(x => x.DiscountAmount)
                .GreaterThan(0).WithMessage("Discount amount must be greater than zero.");

            RuleFor(x => x.DiscountType)
                .IsInEnum().WithMessage("Discount type is invalid.");

            RuleFor(x => x.ExpiresAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("Expiration must be set to a future date.");
        }
    }
}
