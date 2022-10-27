using TestClassLibrary.SupportModels;

namespace TestClassLibrary.TestModels
{
    public class TestHashSets
    {
        public HashSet<string>? TestHashSetStrings { get; set; }
        public HashSet<string?>? TestHashSetNullableStrings { get; set; }
        public HashSet<int>? TestHashSetIntss { get; set; }
        public HashSet<int?>? TestHashSetNullableInts { get; set; }
        public HashSet<UserModel>? TestHashSetUserModels { get; set; }
        public List<ListModel>? TestHashSetOfCollections { get; set; }
    }
}
