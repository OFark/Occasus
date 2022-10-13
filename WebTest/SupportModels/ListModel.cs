namespace WebTest.SupportModels
{
    public record ListModel
    {
        public List<string>? ListStrings { get; set; }
        public HashSet<int>? HashSetInts    { get; set; }
        public List<UserModel>? UserModels { get; set; }
    }
}
