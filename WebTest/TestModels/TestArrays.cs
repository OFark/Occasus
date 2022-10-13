using WebTest.SupportModels;

namespace WebTest.TestModels;

public class TestArrays
{
    public string[]? TestArrayStrings { get; set; }
    public string?[]? TestArrayNullableStrings { get; set; }
    public int[]? TestArrayIntss { get; set; }
    public int?[]? TestArrayNullableInts { get; set; }
    public UserModel[]? TestArrayUserModels { get; set; }
    public ListModel[]? TestArrayOfCollections { get; set; }
}
