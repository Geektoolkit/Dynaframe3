using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SkiaSharp;
using Splat;

namespace Dynaframe3.ImagePresenters
{
  public partial class BlurBoxImage : UserControl
  {
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

      // Frontloading some allocations
      blurInfo = new SKImageInfo(640, 480);
      blurredbitmap = new SKBitmap(blurInfo);

      deviceCache = Locator.Current.GetService<DeviceCache>();

      backgroundImage = this.FindControl<Image>("backgroundImage");
      foregroundImage = this.FindControl<Image>("foregroundImage");

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

    public void ImagePan()
    {
      var viewport = new Tuple<double, double>(backgroundImage.Bounds.Height, backgroundImage.Bounds.Width);
      var foreground = new Tuple<double, double>(foregroundImage.Bounds.Height, foregroundImage.Bounds.Width);

      var min = Math.Min(foregroundImage.Source.Size.Height, foregroundImage.Source.Size.Width);
      var ratio = 0.0;

      if (min == foregroundImage.Source.Size.Height)
      {
        ratio = backgroundImage.Bounds.Height / foregroundImage.Source.Size.Height;
      }
      else
      {
        ratio = backgroundImage.Bounds.Width / foregroundImage.Source.Size.Width;
      }

      var height = foregroundImage.Source.Size.Height * ratio;
      var width = foregroundImage.Source.Size.Width * ratio;

      //foregroundImage.Height = height;
      //foregroundImage.Width = width;

      if (height > backgroundImage.Bounds.Height)
      {
        //Transition here
        //foregroundImage.RenderTransform = new TranslateTransform();
        //(foregroundImage.RenderTransform as TranslateTransform).Y = 1;
      }

      if (width > backgroundImage.Bounds.Width)
      {
        //transition here
        //foregroundImage.RenderTransform = new TranslateTransform();
        //(foregroundImage.RenderTransform as TranslateTransform).X = 1;
      }

      //foregroundImage.Source.WhenAnyValue
      //var dimensions = new Tuple<double, double>()
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

      var currentBackground = backgroundImage.Source as IDisposable;
      var currentForeground = foregroundImage.Source as IDisposable;

      backgroundImage.Source = bitmapData;
      foregroundImage.Source = new Bitmap(ImageString);

      ImagePan();

      currentBackground?.Dispose();
      currentForeground?.Dispose();
    }
  }
}