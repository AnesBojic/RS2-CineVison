using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class LanguageUpdateValidator : LookupRequestValidator<LanguageUpdateRequest>
    {
        public LanguageUpdateValidator()
        {
            RuleFor(x => x.Code)
                .MaximumLength(10).WithMessage("Code cannot exceed 10 characters.");
        }
    }
}
