using Microsoft.AspNetCore.Components;
using Occasus.Settings;

namespace Occasus.Shared
{
    public partial class Messages
    {
        [Inject] OccasusMessageStore MessageStore { get; set; } = default!;


        protected override void OnInitialized()
        {
            MessageStore.OnChange += MessagesHaveChanged;
        }

        private void MessagesHaveChanged(object? messages, EventArgs _)
        {
            InvokeAsync(StateHasChanged);
        }
    }
}
