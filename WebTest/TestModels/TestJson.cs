using Occasus.Attributes;

namespace WebTest.TestModels
{
    [RestartRequired]
    public record TestJson
    {
        public string AppName { get; set; }
        public string AppVersion { get; set; }
        public bool IsTest { get; set; }
        public int TestId { get; set; }
    }
}
