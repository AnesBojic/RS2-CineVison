using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class LanguageInsertValidator : LookupRequestValidator<LanguageInsertRequest>
    {
        public LanguageInsertValidator()
        {
            RuleFor(x => x.Code)
                .MaximumLength(10).WithMessage("Code cannot exceed 10 characters.");
        }
    }
}
