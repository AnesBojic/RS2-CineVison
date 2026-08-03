using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class AgeRatingInsertValidator : LookupRequestValidator<AgeRatingInsertRequest>
    {
        public AgeRatingInsertValidator()
        {
            RuleFor(x => x.MinimumAge)
                .InclusiveBetween(0, 21).WithMessage("Minimum age must be between 0 and 21.")
                .When(x => x.MinimumAge.HasValue);
        }
    }
}
