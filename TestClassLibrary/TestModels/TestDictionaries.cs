using TestClassLibrary.SupportModels;

namespace TestClassLibrary.TestModels;

public class TestDictionaries
{
    public Dictionary<string, string>? TestDictionaryStringStrings { get; set; }
    public Dictionary<string, int>? TestDictionaryStringInts { get; set; }
    public Dictionary<int, string>? TestDictionaryIntStrings { get; set; }
    public Dictionary<int, int>? TestDictionaryIntInts { get; set; }
    public Dictionary<string, UserModel>? TestDictionaryStringUserModel { get; set; }
    public Dictionary<string, ListModel>? TestDictionaryStringCollections { get; set; }
    public Dictionary<string, DictionaryModel>? TestDictionaryStringDictionary { get; set; }
}
