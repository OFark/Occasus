using MudBlazor;
using Occasus.Attributes;
using System.ComponentModel.DataAnnotations;

namespace TestClassLibrary.TestModels
{
    [Display(Name = "A Simple Test")]
    public record TestSimple
    {
        [Display(Name = "A Simple Test String"), Required]
        public string? TestString { get; set; }
        [Input(InputType.Password)]
        public string? TestPassword { get; set; }
        public int TestInteger { get; set; }
        public int TestNullableInteger { get; set; }
        public decimal TestDecimal { get; set; }
        public decimal? TestNullableDecimal { get; set; }
        public float TestFloat { get; set; }
        public float? TestNullableFloat { get; set; }
        public double TestDouble { get; set; }
        public double TestNullableDouble { get; set; }
        [RestartRequired]
        public bool TestBoolean { get; set; }
        public bool? TestNullableBool { get; set; }

    }
}
