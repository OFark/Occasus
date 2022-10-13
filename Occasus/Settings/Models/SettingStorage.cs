namespace Occasus.Settings.Models
{
    public class SettingStorage
    {
        public SettingStorage(string name, string? value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string? Value { get; set; }
    }
}
