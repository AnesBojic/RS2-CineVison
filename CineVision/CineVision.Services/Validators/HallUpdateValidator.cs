using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class HallUpdateValidator : AbstractValidator<HallUpdateRequest>
    {
        public HallUpdateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
        }
    }
}
