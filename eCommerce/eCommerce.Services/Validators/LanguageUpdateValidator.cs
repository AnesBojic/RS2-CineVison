using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
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
