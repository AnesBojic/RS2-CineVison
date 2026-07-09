using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class SeatUpdateValidator : AbstractValidator<SeatUpdateRequest>
    {
        public SeatUpdateValidator()
        {
            RuleFor(x => x.RowLabel)
                .NotEmpty().WithMessage("Row label is required.")
                .MaximumLength(5).WithMessage("Row label cannot exceed 5 characters.");

            RuleFor(x => x.SeatNumber)
                .GreaterThan(0).WithMessage("Seat number must be greater than 0.");

            RuleFor(x => x.SeatType)
                .InclusiveBetween(0, 1).WithMessage("Seat type must be 0 (Regular) or 1 (VIP).");
        }
    }
}
