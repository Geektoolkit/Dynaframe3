using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace Dynaframe3
{
    class Program
    {

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static int Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp()
                  .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
            }
            catch (PlatformNotSupportedException)
            {
                // This fires when we call thread.abort. This is a hack until I know
                // how to get the cancellation token to fire in the httplistener
                // To quote Dr. Banner "Well this is horrible". I know.
                throw new Exception("Thankyou for using Dynaframe!");
            }
            return 0;
        }

        // Avalonia configuration, don't remove; also used by visual designer.
         public static AppBuilder BuildAvaloniaApp()
             => AppBuilder.Configure<App>().UsePlatformDetect().With(new AvaloniaNativePlatformOptions { UseGpu = true }).LogToTrace();

        // Joe - Storing this here as it's a way to test with a disabled GPU if needed.
        //public static AppBuilder BuildAvaloniaApp()
        //  => AppBuilder.Configure<App>().UsePlatformDetect().With(new X11PlatformOptions { UseGpu = false, UseEGL = false }).LogToTrace();

    }
}
