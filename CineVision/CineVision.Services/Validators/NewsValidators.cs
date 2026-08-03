using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class NewsInsertValidator : AbstractValidator<NewsInsertRequest>
    {
        public NewsInsertValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.")
                .MaximumLength(4000).WithMessage("Content cannot exceed 4000 characters.");
        }
    }

    public class NewsUpdateValidator : AbstractValidator<NewsUpdateRequest>
    {
        public NewsUpdateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.")
                .MaximumLength(4000).WithMessage("Content cannot exceed 4000 characters.");
        }
    }
}
