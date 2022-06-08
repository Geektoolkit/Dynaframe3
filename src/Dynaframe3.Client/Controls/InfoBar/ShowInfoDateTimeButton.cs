using Dynaframe3.Shared;

namespace Dynaframe3.Client.Controls.InfoBar
{
    public class ShowInfoDateTimeButton : ToggleButtonBase
    {
        protected override string GetButtonText()
        {
            if (GetCurrentToggle())
            {
                return "Hide Date & Time";
            }
            return "Show Date & Time";
        }

        protected override string GetCommand()
        {
            if (GetCurrentToggle())
            {
                return "INFOBAR_DATETIME_Off";
            }
            return "INFOBAR_DATETIME_On";
        }

        protected override bool GetToggle(AppSettings settings)
            => settings.ShowInfoDateTime;
    }
}
