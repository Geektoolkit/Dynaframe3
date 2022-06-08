using Dynaframe3.Client.Services;
using Dynaframe3.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dynaframe3.Client.Controls
{
    public abstract class ToggleButtonBase : ComponentBase, IDisposable
    {
        [Inject]
        private StateContainer State { get; set; }

        [Inject]
        private DevicesService Service { get; set; }

        protected AppSettings AppSettings { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            AppSettings = State.CurrentAppSettings;
            State.OnUpdated += OnStateUpdated;
        }

        protected virtual async void OnStateUpdated(AppSettings settings)
        {
            var currentToggle = GetCurrentToggle();
            var newToggle = GetToggle(settings);
            AppSettings = settings;

            if (currentToggle != newToggle)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);

            if (AppSettings is not null)
            {
                builder.OpenElement(0, "button");

                var toggle = GetCurrentToggle();

                builder.AddAttribute(1, "class", toggle ? "btn btn-success btn-lg" : "btn btn-primary btn-lg");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, ToggleAsync));
                var textContent = GetButtonText();
                builder.AddContent(3, GetButtonText());
                builder.CloseElement();
            }
        }

        protected abstract string GetCommand();

        protected abstract bool GetToggle(AppSettings settings);

        protected bool GetCurrentToggle()
            => GetToggle(AppSettings);

        protected abstract string GetButtonText();

        protected virtual async Task ToggleAsync()
        {
            await Service.ExecuteCommandAsync(State.CurrentDeviceId, GetCommand());
        }

        public void Dispose()
            => State.OnUpdated -= OnStateUpdated;
    }
}
