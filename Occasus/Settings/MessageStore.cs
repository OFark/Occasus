namespace Occasus.Settings
{
    public class OccasusMessageStore
    {
        readonly List<string> _messages = new();

        public IReadOnlyList<string> Messages => _messages.AsReadOnly();

        public void Add(string message, bool add = true)
        {
            if (add && !Messages.Contains(message))
            {
                _messages.Add(message);
                OnChange?.Invoke(Messages, new());
            }

            if(!add && Messages.Contains(message))
            {
                _messages.Remove(message);
                OnChange?.Invoke(Messages, new());
            }
        }

        public EventHandler? OnChange;
    }
}
