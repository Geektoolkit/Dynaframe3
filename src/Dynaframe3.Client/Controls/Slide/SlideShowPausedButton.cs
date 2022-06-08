using Dynaframe3.Shared;
using System;

namespace Dynaframe3.Client.Controls.Slide
{
    public class SlideShowPausedButton : ToggleButtonBase
    {
        protected override string GetButtonText()
        {
            if (GetCurrentToggle())
            {
                return "Paused";
            }
            return "Playing";
        }

        protected override string GetCommand()
        {
            if (GetCurrentToggle())
            {
                return "CONTROL_PAUSE_Off";
            }
            return "CONTROL_PAUSE_On";
        }

        protected override bool GetToggle(AppSettings settings)
            => settings.SlideShowPaused;
    }
}
