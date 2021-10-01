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

            blurPaint = new SKPaint();
            blurPaint.ImageFilter = SKImageFilter.CreateBlur(AppSettings.Default.BlurBoxSigmaX, AppSettings.Default.BlurBoxSigmaY);

            backgroundImage.Margin = new Thickness(AppSettings.Default.BlurBoxMargin);
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


            if (!String.IsNullOrEmpty(ImageString))
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
            ImageString = newImagePath;
            backgroundImage.Margin = new Thickness(AppSettings.Default.BlurBoxMargin);
            blurPaint.ImageFilter = SKImageFilter.CreateBlur(AppSettings.Default.BlurBoxSigmaX, AppSettings.Default.BlurBoxSigmaY);
            ShowBitmapWithBlurbox();
        }


        private void ShowBitmapWithBlurbox()
        {
            SKBitmap bitmap = SKBitmap.Decode(ImageString);
            bitmap.ScalePixels(blurredbitmap, SKFilterQuality.Medium);

            SKCanvas canvas = new SKCanvas(blurredbitmap);
            canvas.DrawBitmap(blurredbitmap, new SKPoint(0,0), blurPaint);

            SKData data = SKImage.FromBitmap(blurredbitmap).Encode(SKEncodedImageFormat.Jpeg, 100);

            bitmapData = new Bitmap(data.AsStream());

            backgroundImage.Source = bitmapData;
            foregroundImage.Source = new Bitmap(ImageString);

            bitmap.Dispose();
            canvas.Dispose();
            data.Dispose();
        }
    }
}