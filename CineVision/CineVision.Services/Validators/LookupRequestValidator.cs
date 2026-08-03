using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    /// <summary>
    /// Validates the fields shared by every reference-data request. Reference names are short
    /// codes as well as words ("G", "3D", "PG-13"), so only emptiness is rejected.
    /// </summary>
    public class LookupRequestValidator<T> : AbstractValidator<T> where T : LookupRequest
    {
        public LookupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(80).WithMessage("Name cannot exceed 80 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(300).WithMessage("Description cannot exceed 300 characters.");
        }
    }
}
