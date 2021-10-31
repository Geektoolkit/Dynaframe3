using Dynaframe3.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Dynaframe3.Client.Pages
{
    public class AppSettingsPage : ComponentBase, IDisposable
    {
        protected AppSettings appSettings;

        [Inject]
        public StateContainer State { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            appSettings = await State.GetCurrentSettingsAsync();

            State.OnUpdated += OnSettingsUpdated;
        }

        protected virtual async void OnSettingsUpdated(AppSettings appSettings)
        {
            this.appSettings = appSettings;
            await InvokeAsync(StateHasChanged);
        }

        public virtual void Dispose()
        {
            State.OnUpdated -= OnSettingsUpdated;
        }
    }
}
