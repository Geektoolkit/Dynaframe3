using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;
using Splat;

namespace Dynaframe3.ImagePresenters
{
    public partial class BlurBoxImage : UserControl
    {
        Image backgroundImage;
        Image foregroundImage;

        Bitmap bitmapData;
        SKImageInfo blurInfo;
        SKBitmap blurredbitmap;
        SKPaint blurPaint;

        readonly DeviceCache deviceCache;

        public string ImageString { get; set; }
        public BlurBoxImage()
        {
            ClipToBounds = false;
            InitializeComponent();
            ImageString = "";
            backgroundImage = this.FindControl<Image>("backgroundImage");
            foregroundImage = this.FindControl<Image>("foregroundImage");

            // Frontloading some allocations
            blurInfo = new SKImageInfo(640, 480);
            blurredbitmap = new SKBitmap(blurInfo);

            deviceCache = Locator.Current.GetService<DeviceCache>();

            Initialized += BlurBoxImage_Initialized;
        }

        private void BlurBoxImage_Initialized(object sender, EventArgs e)
        {
            var appSettings = deviceCache.CurrentDevice.AppSettings;

            blurPaint = new SKPaint();
            blurPaint.ImageFilter = SKImageFilter.CreateBlur(appSettings.BlurBoxSigmaX, appSettings.BlurBoxSigmaY);

            backgroundImage.Margin = new Thickness(appSettings.BlurBoxMargin);
        }

        /// <summary>
        /// Set the 'stretch' of the foreground image
        /// </summary>
        /// <param name="stretch"></param>
        public void SetImageStretch(Stretch stretch)
        {
            foregroundImage.Stretch = stretch;
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);


            if (!string.IsNullOrEmpty(ImageString))
            {
                ShowBitmapWithBlurbox();
            }
        }
        public void UpdateImage()
        {
            ShowBitmapWithBlurbox();
        }

        public void UpdateImage(string newImagePath)
        {
            var appSettings = deviceCache.CurrentDevice.AppSettings;
            ImageString = newImagePath;
            backgroundImage.Margin = new Thickness(appSettings.BlurBoxMargin);
            blurPaint.ImageFilter = SKImageFilter.CreateBlur(appSettings.BlurBoxSigmaX, appSettings.BlurBoxSigmaY);
            ShowBitmapWithBlurbox();
        }


        private void ShowBitmapWithBlurbox()
        {
            using SKBitmap bitmap = SKBitmap.Decode(ImageString);
            if (bitmap is null)
            {
                throw new InvalidOperationException($"Could not load file {ImageString}");
            }

            bitmap.ScalePixels(blurredbitmap, SKFilterQuality.Medium);

            using SKCanvas canvas = new SKCanvas(blurredbitmap);
            canvas.DrawBitmap(blurredbitmap, new SKPoint(0, 0), blurPaint);

            using SKData data = SKImage.FromBitmap(blurredbitmap).Encode(SKEncodedImageFormat.Jpeg, 100);

            bitmapData = new Bitmap(data.AsStream());

            backgroundImage.Source = bitmapData;
            foregroundImage.Source = new Bitmap(ImageString);
        }
    }
}