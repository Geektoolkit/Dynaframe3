using Dynaframe3.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dynaframe3.Client.Controls.Frame
{
    public class ScreenCommandButton : ToggleButtonBase
    {
        protected override string GetButtonText()
        {
            if (GetCurrentToggle())
            {
                return "Turn Screen Off";
            }
            return "Turn Screen On";
        }

        protected override string GetCommand()
        {
            if (GetCurrentToggle())
            {
                return "SCREENOFF";
            }
            return "SCREENON";
        }

        protected override bool GetToggle(AppSettings settings)
            => settings.ScreenStatus;
    }
}
