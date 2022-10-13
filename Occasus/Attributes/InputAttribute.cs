using MudBlazor;

namespace Occasus.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class InputAttribute : Attribute
    {
        public InputAttribute(InputType inputType)
        {
            InputType = inputType;
        }

        public InputType InputType { get; init; }
    }
}
