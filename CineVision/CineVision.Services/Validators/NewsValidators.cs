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

            RuleFor(x => x.ImageBase64)
                .NotEmpty().WithMessage("News image is required.")
                .Must(base64 => ImageContentValidator.TryValidateBase64(base64!, out _, out _))
                .WithMessage("News image must be a valid JPEG, PNG, GIF, or WebP file.")
                .When(x => !string.IsNullOrWhiteSpace(x.ImageBase64));
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

            RuleFor(x => x.ImageBase64)
                .NotEmpty().WithMessage("News image is required.")
                .Must(base64 => ImageContentValidator.TryValidateBase64(base64!, out _, out _))
                .WithMessage("News image must be a valid JPEG, PNG, GIF, or WebP file.")
                .When(x => !string.IsNullOrWhiteSpace(x.ImageBase64));
        }
    }
}
