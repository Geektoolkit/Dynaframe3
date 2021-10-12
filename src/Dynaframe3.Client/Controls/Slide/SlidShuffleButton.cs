using Dynaframe3.Shared;
using System;

namespace Dynaframe3.Client.Controls.Slide
{
    public class SlidShuffleButton : ToggleButtonBase
    {
        protected override string GetButtonText()
        {
            if (GetCurrentToggle())
            {
                return "Shuffle On";
            }
            return "Shuffle Off";
        }

        protected override string GetCommand()
        {
            if (GetCurrentToggle())
            {
                return "SHUFFLE_OFF";
            }
            return "SHUFFLE_ON";
        }

        protected override bool GetToggle(AppSettings settings)
            => settings.Shuffle;
    }
}
