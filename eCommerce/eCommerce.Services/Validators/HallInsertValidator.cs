using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class HallInsertValidator : AbstractValidator<HallInsertRequest>
    {
        public HallInsertValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

            RuleFor(x => x.RowsCount)
                .GreaterThan(0).WithMessage("Number of rows must be at least 1.")
                .LessThanOrEqualTo(26).WithMessage("Number of rows cannot exceed 26.");

            RuleFor(x => x.SeatsPerRow)
                .GreaterThan(0).WithMessage("Number of columns must be at least 1.")
                .LessThanOrEqualTo(50).WithMessage("Number of columns cannot exceed 50.");
        }
    }
}
