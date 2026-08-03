using CineVision.Model.Requests;
using FluentValidation;
namespace CineVision.Services.Validators
{
    public class HallSeatLayoutUpdateValidator : AbstractValidator<HallSeatLayoutUpdateRequest>
    {
        public HallSeatLayoutUpdateValidator()
        {
            RuleFor(x => x.Seats)
                .NotNull().WithMessage("No seat layout was provided.")
                .Must(seats => seats != null && seats.Count > 0)
                .WithMessage("No seat layout was provided.");

            RuleForEach(x => x.Seats).ChildRules(seat =>
            {
                seat.RuleFor(s => s.SeatId)
                    .GreaterThan(0).WithMessage("Each seat id must be greater than 0.");

                seat.RuleFor(s => s.SeatType)
                    .Must(t => t is 0 or 2)
                    .WithMessage("Seat type must be 0 (Regular) or 2 (Couple).");
            }).When(x => x.Seats != null && x.Seats.Count > 0);
        }
    }
}
