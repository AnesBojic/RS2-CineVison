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
                .InclusiveBetween(0, 100).WithMessage("Rows count must be between 0 and 100.");

            RuleFor(x => x.SeatsPerRow)
                .InclusiveBetween(0, 100).WithMessage("Seats per row must be between 0 and 100.");
        }
    }
}
