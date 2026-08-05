using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class ReservationCreateValidator : AbstractValidator<ReservationCreateRequest>
    {
        public ReservationCreateValidator()
        {
            RuleFor(x => x.ProjectionId)
                .GreaterThan(0).WithMessage("Projection is required.");

            RuleFor(x => x.SeatIds)
                .NotNull().WithMessage("No seats were selected.")
                .Must(ids => ids != null && ids.Count > 0)
                .WithMessage("No seats were selected.");

            RuleForEach(x => x.SeatIds)
                .GreaterThan(0).WithMessage("Each seat id must be greater than 0.")
                .When(x => x.SeatIds != null && x.SeatIds.Count > 0);

            RuleFor(x => x.PaymentIntentId)
                .MaximumLength(200).WithMessage("Payment intent id cannot exceed 200 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentIntentId));

            RuleFor(x => x.CustomerName)
                .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.CustomerName));

            RuleFor(x => x.CustomerEmail)
                .EmailAddress().WithMessage("Customer email must be a valid email address.")
                .MaximumLength(100).WithMessage("Customer email cannot exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
        }
    }

    public class CreatePaymentIntentValidator : AbstractValidator<CreatePaymentIntentRequest>
    {
        public CreatePaymentIntentValidator()
        {
            RuleFor(x => x.ProjectionId)
                .GreaterThan(0).WithMessage("Projection is required.");

            RuleFor(x => x.SeatIds)
                .NotNull().WithMessage("No seats were selected.")
                .Must(ids => ids != null && ids.Count > 0)
                .WithMessage("No seats were selected.");

            RuleForEach(x => x.SeatIds)
                .GreaterThan(0).WithMessage("Each seat id must be greater than 0.")
                .When(x => x.SeatIds != null && x.SeatIds.Count > 0);
        }
    }

    public class ReservationCancelValidator : AbstractValidator<ReservationCancelRequest>
    {
        public ReservationCancelValidator()
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }
}
