using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
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
                .Must(base64 =>
                {
                    if (string.IsNullOrWhiteSpace(base64))
                    {
                        return true;
                    }

                    return ImageContentValidator.TryValidateBase64(base64, out _, out _);
                })
                .WithMessage("Image must be a valid JPEG, PNG, GIF, or WebP file.")
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
                .Must(base64 =>
                {
                    if (string.IsNullOrWhiteSpace(base64))
                    {
                        return true;
                    }

                    return ImageContentValidator.TryValidateBase64(base64, out _, out _);
                })
                .WithMessage("Image must be a valid JPEG, PNG, GIF, or WebP file.")
                .When(x => !string.IsNullOrWhiteSpace(x.ImageBase64));
        }
    }
}
