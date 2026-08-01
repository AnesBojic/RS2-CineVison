using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class MoviePosterUpdateValidator : AbstractValidator<MoviePosterUpdateRequest>
    {
        public MoviePosterUpdateValidator()
        {
            RuleFor(x => x.PosterImageBase64)
                .NotEmpty().WithMessage("Poster image is required.");

            RuleFor(x => x.PosterImageBase64)
                .Must(base64 => ImageContentValidator.TryValidateBase64(base64!, out _, out _))
                .WithMessage("Poster image must be a valid JPEG, PNG, GIF, or WebP file.")
                .When(x => !string.IsNullOrWhiteSpace(x.PosterImageBase64));
        }
    }
}
