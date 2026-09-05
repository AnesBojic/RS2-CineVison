using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class ProjectionCancelValidator : AbstractValidator<ProjectionCancelRequest>
    {
        public ProjectionCancelValidator()
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }
}
