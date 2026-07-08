using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ScreeningUpdateValidator : AbstractValidator<ScreeningUpdateRequest>
    {
        public ScreeningUpdateValidator()
        {
            RuleFor(x => x.MovieId)
                .GreaterThan(0).WithMessage("MovieId is required and must be greater than 0.");

            RuleFor(x => x.HallId)
                .GreaterThan(0).WithMessage("HallId is required and must be greater than 0.");

            RuleFor(x => x.BasePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Base price cannot be negative.");
        }
    }
}
