using Microsoft.Extensions.Options;
using TestClassLibrary.TestModels;

namespace TestClassLibrary
{
    public class TestSimpleValidator : IValidateOptions<TestSimple>
    {
        public ValidateOptionsResult Validate(string? name, TestSimple options)
        {
            return options.TestString is not null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail("TestString value is required");
        }
    }
}
