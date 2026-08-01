using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class AgeRatingUpdateValidator : LookupRequestValidator<AgeRatingUpdateRequest>
    {
        public AgeRatingUpdateValidator()
        {
            RuleFor(x => x.MinimumAge)
                .InclusiveBetween(0, 21).WithMessage("Minimum age must be between 0 and 21.")
                .When(x => x.MinimumAge.HasValue);
        }
    }
}
