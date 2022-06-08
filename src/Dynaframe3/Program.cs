using Avalonia;

namespace Dynaframe3.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
                  .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>().UsePlatformDetect().With(new AvaloniaNativePlatformOptions { UseGpu = true }).LogToTrace();
    }
}
