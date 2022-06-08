using Dynaframe3.Shared;
using System;

namespace Dynaframe3.Client.Controls.InfoBar
{
    public class ShowInfoIpButton : ToggleButtonBase
    {
        protected override string GetButtonText()
        {
            if (GetCurrentToggle())
            {
                return "Hide IP";
            }
            return "Show IP";
        }

        protected override string GetCommand()
        {
            if (GetCurrentToggle())
            {
                return "INFOBAR_IP_Off";
            }
            return "INFOBAR_IP_On";
        }

        protected override bool GetToggle(AppSettings settings)
            => settings.ShowInfoIP.Equals("true", StringComparison.InvariantCultureIgnoreCase);
    }
}
