using System.ComponentModel.DataAnnotations;

namespace Occasus.Settings
{
    public class CompositeValidationAttribute : ValidationAttribute
    {

        private readonly List<ValidationAttribute> _attributes = new();

        public CompositeValidationAttribute(IEnumerable<ValidationAttribute> attributes)
        {
            _attributes = attributes.ToList();
        }
        public override bool RequiresValidationContext => _attributes.Any(x => x.RequiresValidationContext);
        public override bool IsValid(object? value) => _attributes.All(x => x.IsValid(value));

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => _attributes.FirstOrDefault(x => !x.IsValid(value))?.GetValidationResult(value, validationContext);

    }
}
