using Dynaframe3.Shared;

namespace Dynaframe3.Client.Controls.InfoBar
{
    public class ShowInfoExifButton : ToggleButtonBase
    {
        protected override string GetButtonText()
        {
            if (GetCurrentToggle())
            {
                return "Hide EXIF Data";
            }
            return "Show EXIF Data";
        }

        protected override string GetCommand()
        {
            if (GetCurrentToggle())
            {
                return "INFOBAR_EXIF_Off";
            }
            return "INFOBAR_EXIF_On";
        }

        protected override bool GetToggle(AppSettings settings)
            => settings.ShowEXIFData;
    }
}
