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
        public Stretch Stretch = Stretch.None;

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
            public SKBitmap sourceBitmap = null;
            public SKBitmap blurredbitmap = null;
            public SKBitmap scaledBitmap = null;
            public string imagePath = "";
            public Stretch imageStretch;

            public CustomDrawOp(Rect bounds, FormattedText noSkia, string imagePath, Stretch imageStretch)
            {
                _noSkia = noSkia;
                Bounds = bounds;
                this.imagePath = imagePath;
                this.imageStretch = imageStretch;
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

                if (sourceBitmap == null)
                {
                    sourceBitmap = SKBitmap.Decode(imagePath);
                    blurredbitmap = new SKBitmap(info);
                }


                // Calculate stretch
                SKImageInfo stretchInfo;

                switch (imageStretch)
                {
                    case Stretch.None:
                        {
                            stretchInfo = new SKImageInfo(sourceBitmap.Width, sourceBitmap.Height);
                            break;
                        }
                    case Stretch.Fill:
                        {
                            stretchInfo = new SKImageInfo((int)Bounds.Width, (int)Bounds.Height);
                            break;
                        }
                    case Stretch.Uniform:
                        {
                            float scaleFactor = 1;
 
                            scaleFactor = (float)(Bounds.Height / sourceBitmap.Height);
                            if (sourceBitmap.Width * scaleFactor <= Bounds.Width)
                            {
                                stretchInfo = new SKImageInfo((int)(scaleFactor * sourceBitmap.Width), (int)(scaleFactor * sourceBitmap.Height));
                            }
                            else
                            {
                                scaleFactor = (float)(Bounds.Width / sourceBitmap.Width);
                                stretchInfo = new SKImageInfo((int)(scaleFactor * sourceBitmap.Width), (int)(scaleFactor * sourceBitmap.Height));
                            }

                            break;
                        }
                    case Stretch.UniformToFill:
                        {
                            float scaleFactor = 1;
                            scaleFactor = (float)(Bounds.Height / sourceBitmap.Height);
                            if (sourceBitmap.Width * scaleFactor > Bounds.Width)
                            {
                                stretchInfo = new SKImageInfo((int)(scaleFactor * sourceBitmap.Width), (int)(scaleFactor * sourceBitmap.Height));
                            }
                            else
                            {
                                scaleFactor = (float)(Bounds.Width / sourceBitmap.Width);
                                stretchInfo = new SKImageInfo((int)(scaleFactor * sourceBitmap.Width), (int)(scaleFactor * sourceBitmap.Height));
                            }
                            break;
                        }
                    default:
                        {
                            stretchInfo = new SKImageInfo(sourceBitmap.Width, sourceBitmap.Height);
                            break;
                        }
                }
                if (scaledBitmap == null)
                {
                    scaledBitmap = new SKBitmap(stretchInfo);
                }

                var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;
                if (canvas == null)
                    context.DrawText(Brushes.Black, new Point(), _noSkia.PlatformImpl);
                else
                {

                    canvas.Clear();

                    // Get Center Coordinates
                    float x = (float)(Bounds.Width - scaledBitmap.Width) / 2;
                    float y = (float)(Bounds.Height - scaledBitmap.Height) / 2;


                    // scale blur
                    sourceBitmap.ScalePixels(blurredbitmap, SKFilterQuality.High);

                    // scale main image
                    sourceBitmap.ScalePixels(scaledBitmap, SKFilterQuality.High);

                    // blur temp
                    paint.ImageFilter = SKImageFilter.CreateBlur(40, 40);

                    canvas.DrawBitmap(blurredbitmap, 0, 0, paint);
                    canvas.DrawBitmap(scaledBitmap, x, y);
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
            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), noSkia, imagePath, Stretch));



            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }

    }
}
