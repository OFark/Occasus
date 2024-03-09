using TestClassLibrary.SupportModels;

namespace TestClassLibrary.TestModels;

public record TestComplex
{
    public DateTime TestDateTime { get; set; }
    public DateTime? TestNullableDateTime { get; set; }
    public DateOnly? TestDateOnly { get; set; }
    public TimeSpan TestTimeSpan { get; set; }
    public TimeSpan? TestNullableTimeSpan { get; set; }
    public TimeOnly? TestTimeOnly { get; set; }

    public UserModel? TestUserModel { get; set; }

    public UserModel? TestInternalUserModel { get; internal set; }

}

