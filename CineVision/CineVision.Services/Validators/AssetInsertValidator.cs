using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class AssetInsertValidator : AbstractValidator<AssetInsertRequest>
    {
        public AssetInsertValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("FileName is required.")
                .MaximumLength(100).WithMessage("FileName cannot exceed 100 characters.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("ContentType is required.")
                .MaximumLength(100).WithMessage("ContentType cannot exceed 100 characters.")
                .Must(ct => ImageContentValidator.AllowedContentTypes.Contains(
                    ct.Trim().ToLowerInvariant() is "image/jpg" ? "image/jpeg" : ct.Trim().ToLowerInvariant()))
                .WithMessage("ContentType must be image/jpeg, image/png, image/gif, or image/webp.");

            RuleFor(x => x.Base64Content)
                .NotEmpty().WithMessage("Base64Content is required.")
                .Custom((base64, context) =>
                {
                    if (!ImageContentValidator.TryValidateBase64(base64, out var detected, out var error))
                    {
                        context.AddFailure(error);
                        return;
                    }

                    var claimed = context.InstanceToValidate.ContentType;
                    if (!ImageContentValidator.ContentTypeMatches(claimed, detected))
                    {
                        context.AddFailure(
                            $"ContentType '{claimed}' does not match the actual file content ({detected}).");
                    }
                });

            RuleFor(x => x.MovieId)
                .GreaterThan(0).WithMessage("MovieId is required and must be greater than 0.");
        }
    }
}
