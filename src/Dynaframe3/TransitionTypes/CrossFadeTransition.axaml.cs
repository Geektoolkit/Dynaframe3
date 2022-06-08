using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Dynaframe3.ImagePresenters;
using Dynaframe3.Shared;
using Splat;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dynaframe3.TransitionTypes
{
    public partial class CrossFadeTransition : UserControl
    {
        BlurBoxImage ForegroundImage;
        BlurBoxImage BackgroundImage;

        // Transitions used for animating the fades
        DoubleTransition foregroundTransition;
        DoubleTransition backgroundTransition;

        readonly DeviceCache appSettingsManager;

        public CrossFadeTransition()
        {
            appSettingsManager = Locator.Current.GetService<DeviceCache>()!;

            InitializeComponent();
            BackgroundImage = this.FindControl<BlurBoxImage>("BackgroundBlurBox");
            ForegroundImage = this.FindControl<BlurBoxImage>("ForegroundBlurBox");

            SetTransitions();

            BackgroundImage.Opacity = 0;
            ForegroundImage.Opacity = 1;
        }

        public void SetTransitions()
        {
            // Setup animations
            if (ForegroundImage.Transitions == null)
            {
                ForegroundImage.Transitions = new Transitions();
            }
            if (BackgroundImage.Transitions == null)
            {
                BackgroundImage.Transitions = new Transitions();
            }

            foregroundTransition = new DoubleTransition();
            foregroundTransition.Easing = new QuadraticEaseIn();

            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var appSettings = appSettingsManager.CurrentDevice.AppSettings;

                foregroundTransition.Duration = TimeSpan.FromMilliseconds(appSettings.FadeTransitionTime);
                foregroundTransition.Property = BlurBoxImage.OpacityProperty;

                backgroundTransition = new DoubleTransition();
                backgroundTransition.Easing = new QuadraticEaseIn();
                backgroundTransition.Duration = TimeSpan.FromMilliseconds(appSettings.FadeTransitionTime);
                backgroundTransition.Property = UserControl.OpacityProperty;

                ForegroundImage.Transitions.Clear();
                BackgroundImage.Transitions.Clear();
                ForegroundImage.Transitions.Add(foregroundTransition);
                BackgroundImage.Transitions.Add(backgroundTransition);
            });
        }

        public void SetImage(string newImage, int millisecondsDelay)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    BackgroundImage.UpdateImage(newImage);
                    ForegroundImage.Opacity = 0;
                    BackgroundImage.Opacity = 1;
                }
                catch (Exception exc)
                {
                    Logger.LogComment("Exception in SetImage: " + exc.ToString());
                }
            });


            Thread.Sleep(millisecondsDelay);

            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    ForegroundImage.UpdateImage(newImage);
                    ForegroundImage.Transitions.Clear();
                    BackgroundImage.Transitions.Clear();
                    ForegroundImage.Opacity = 1;
                    BackgroundImage.Opacity = 0;
                    //BackgroundImage.UpdateImage(null);
                    SetTransitions();
                }
                catch (Exception exc)
                {
                    Logger.LogComment("Exception in SetImage (post transition): " + exc.ToString());
                }
            });

        }
        public void SetImageStretch(Stretch stretch)
        {
            ForegroundImage.SetImageStretch(stretch);
            BackgroundImage.SetImageStretch(stretch);

        }
        public void CrossFade()
        {
            Logger.LogComment("Crossfade called");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
