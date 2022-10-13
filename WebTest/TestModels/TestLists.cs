using WebTest.SupportModels;

namespace WebTest.TestModels;

public class TestLists
{
    public List<string>? TestListStrings { get; set; }
    public List<string?>? TestListNullableStrings { get; set; }
    public List<int>? TestListInts { get; set; }
    public List<int?>? TestListNullableInts { get; set; }
    public List<DateTime>? TestListDateTimes { get; set; }
    public List<UserModel>? TestListUserModels { get; set; }
    public List<ListModel>? TestListOfCollections { get; set; }
}
