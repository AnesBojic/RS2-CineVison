using CineVision.Model;
using CineVision.Model.Requests;
using FluentValidation;

namespace CineVision.Services.Validators
{
    public class RoleInsertValidator : LookupRequestValidator<RoleInsertRequest>
    {
        public RoleInsertValidator()
        {
            AddColorRule();
        }

        private void AddColorRule()
        {
            RuleFor(x => x.Color)
                .Must(value => string.IsNullOrWhiteSpace(value) || RolePermissionNames.IsHexColor(value))
                .WithMessage("Color must be a hex value like #2563EB.");
        }
    }

    public class RoleUpdateValidator : LookupRequestValidator<RoleUpdateRequest>
    {
        public RoleUpdateValidator()
        {
            RuleFor(x => x.Color)
                .Must(value => string.IsNullOrWhiteSpace(value) || RolePermissionNames.IsHexColor(value))
                .WithMessage("Color must be a hex value like #2563EB.");
        }
    }
}
