using FluentValidation;

namespace Speedway.Core
{
    public class SpeedwayManifestValidator : AbstractValidator<SpeedwayManifest>
    {
        public SpeedwayManifestValidator()
        {
            RuleFor(x => x.Slug).MaximumLength(20);
            RuleFor(x => x.Developers).NotEmpty();
        }
    }
}