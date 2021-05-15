using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;


namespace Dynaframe3
{
    public partial class BlurBoxImage : UserControl
    {
        public string imagePath = "";
        public BlurBoxImage()
        {
            InitializeComponent();
        }

        public BlurBoxImage(string image)
        {
            imagePath = image;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        class CustomDrawOp : ICustomDrawOperation
        {
            private readonly FormattedText _noSkia;
            public SKBitmap bitmap1 = null;
            public SKBitmap bitmap2 = null;
            public SKBitmap temp = null;
            public string imagePath = "";

            public CustomDrawOp(Rect bounds, FormattedText noSkia, string imagePath)
            {
                _noSkia = noSkia;
                Bounds = bounds;
                this.imagePath = imagePath;
            }

            public void Dispose()
            {
                // No-op
            }

            public Rect Bounds { get; }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation other) => false;
            static Stopwatch St = Stopwatch.StartNew();
            public void Render(IDrawingContextImpl context)
            {
                SKImageInfo info = new SKImageInfo((int)Bounds.Width, (int)Bounds.Height);
                SKPaint paint = new SKPaint();

                if (bitmap1 == null)
                {
                    bitmap1 = SKBitmap.Decode(imagePath);
                    bitmap2 = SKBitmap.Decode(imagePath);
                    temp = new SKBitmap(info);

                }
                var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;
                if (canvas == null)
                    context.DrawText(Brushes.Black, new Point(), _noSkia.PlatformImpl);
                else
                {

                    canvas.Clear();
                    float x = (float)(Bounds.Width - bitmap2.Width) / 2;
                    float y = (float)(Bounds.Height - bitmap2.Height) / 2;



                    bitmap2.ScalePixels(temp, SKFilterQuality.High);

                    // blur temp
                    paint.ImageFilter = SKImageFilter.CreateBlur(40, 40);

                    canvas.DrawBitmap(temp, 0, 0, paint);
                    canvas.DrawBitmap(bitmap2, x, y);
                }
            }
            static int Animate(int d, int from, int to)
            {
                return 0;
            }
        }

        public override void Render(DrawingContext context)
        {
            var noSkia = new FormattedText()
            {
                Text = "Current rendering API is not Skia"
            };
            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), noSkia, imagePath));
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }

    }
}
