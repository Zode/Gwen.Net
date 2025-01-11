using System;
using Gwen.Net.Control;
using Gwen.Net.OpenTk.Renderers;
using SkiaSharp;
using Bitmap = SkiaSharp.SKBitmap;

namespace Gwen.Net.OpenTk
{
    public sealed class TextRenderer : IDisposable
    {
        private readonly Bitmap bitmap;
        private readonly SKCanvas graphics;
        private readonly Texture texture;
        private readonly SKPaint paint;
        private bool disposed;

        public Texture Texture => texture;

        public TextRenderer(int width, int height, OpenTKRendererBase renderer)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");

            bitmap = new Bitmap(width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
            graphics = new SKCanvas(bitmap);
            //graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            texture = new Texture(renderer)
            {
                Width = width,
                Height = height
            };

            paint = new()
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };
        }

        /// <summary>
        /// Draws the specified string to the backing store.
        /// </summary>
        /// <param name="text">The <see cref="System.String"/> to draw.</param>
        /// <param name="font">The <see cref="SKFont"/> that will be used.</param>
        /// <param name="color">The <see cref="SKColor"/> that will be used.</param>
        /// <param name="point">The location of the text on the backing store, in 2d pixel coordinates.
        /// The origin (0, 0) lies at the top-left corner of the backing store.</param>
        public void DrawString(string text, SKFont font, SKColor color, Point point)
        {
            paint.Color = color;
            graphics.Clear(color.WithAlpha(0));
            graphics.DrawText(text, point.X, point.Y, font, paint);

            OpenTKRendererBase.LoadTextureInternal(texture, bitmap); // copy bitmap to gl texture
        }

        void Dispose(bool manual)
        {
            if (!disposed)
            {
                if (manual)
                {
                    bitmap.Dispose();
                    graphics.Dispose();
                    texture.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#if DEBUG
        ~TextRenderer()
        {
            throw new InvalidOperationException(String.Format("[Warning] Resource leaked: {0}", typeof(TextRenderer)));
        }
#endif
    }
}