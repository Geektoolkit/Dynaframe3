using Dynaframe3.Shared;

namespace Dynaframe3.Client.Controls.InfoBar
{
    public class ShowInfoFileNameButton : ToggleButtonBase
    {
        protected override string GetButtonText()
        {
            if (GetCurrentToggle())
            {
                return "Hide Filename";
            }
            return "Show Filename";
        }

        protected override string GetCommand()
        {
            if (GetCurrentToggle())
            {
                return "INFOBAR_FILENAME_Off";
            }
            return "INFOBAR_FILENAME_On";
        }

        protected override bool GetToggle(AppSettings settings)
            => settings.ShowInfoFileName;
    }
}
