using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class EmailSendValidator : AbstractValidator<EmailSendRequest>
    {
        public EmailSendValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Subject is required.")
                .MaximumLength(200).WithMessage("Subject cannot exceed 200 characters.");

            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Body is required.")
                .MaximumLength(20_000).WithMessage("Body cannot exceed 20000 characters.");
        }
    }
}
