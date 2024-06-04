using MudBlazor;
using Occasus.Attributes;
using TestClassLibrary.SupportModels;

namespace TestClassLibrary.TestModels;

public record TestComplex
{
    public DateTime TestDateTime { get; set; }
    public DateTime? TestNullableDateTime { get; set; }

    [Input(InputType.Date)]
    public DateTime TestDateTimeDateOnly { get; set; }
    public DateOnly TestDateOnly { get; set; }
    public DateOnly? TestNullableDateOnly { get; set; }
    public TimeSpan TestTimeSpan { get; set; }
    public TimeSpan? TestNullableTimeSpan { get; set; }
    public TimeOnly TestTimeOnly { get; set; }
    public TimeOnly? TestNullableTimeOnly { get; set; }

    public UserModel? TestUserModel { get; set; }

    public UserModel? TestInternalUserModel { get; internal set; }

}

