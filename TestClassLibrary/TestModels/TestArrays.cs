using TestClassLibrary.SupportModels;

namespace TestClassLibrary.TestModels;

public class TestArrays
{
    public string[]? TestArrayStrings { get; set; }
    public string?[]? TestArrayNullableStrings { get; set; }
    public int[]? TestArrayInts { get; set; }
    public int?[]? TestArrayNullableInts { get; set; }
    public UserModel[]? TestArrayUserModels { get; set; }
    public ListModel[]? TestArrayOfCollections { get; set; }

    public string[]? TestNoSetterArrayStrings { get; }
}
