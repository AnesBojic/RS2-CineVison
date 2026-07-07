using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators;

public class ProductReviewUpdateValidator : AbstractValidator<ProductReviewUpdateRequest>
{
    public ProductReviewUpdateValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
