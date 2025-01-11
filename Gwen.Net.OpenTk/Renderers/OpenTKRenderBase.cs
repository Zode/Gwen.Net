using System;
using System.Collections.Generic;
using System.IO;
using Gwen.Net.Renderer;
using OpenTK.Graphics.OpenGL;
using SkiaSharp;

using Bitmap = SkiaSharp.SKBitmap;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace Gwen.Net.OpenTk.Renderers
{
    public abstract class OpenTKRendererBase : RendererBase
    {
        protected static int lastTextureID;

        private readonly Dictionary<Tuple<string, Font>, TextRenderer> stringCache;

        protected int drawCallCount;
        protected bool clipEnabled;
        protected bool textureEnabled;
        protected Color color;

        public int TextCacheSize => stringCache.Count;

        public int DrawCallCount => drawCallCount;

        public abstract int VertexCount { get; }

        public int GLVersion { get; }

        public OpenTKRendererBase()
            : base()
        {
            GLVersion = GL.GetInteger(GetPName.MajorVersion) * 10 + GL.GetInteger(GetPName.MinorVersion);

            stringCache = new Dictionary<Tuple<string, Font>, TextRenderer>();
        }

        public override void Dispose()
        {
            FlushTextCache();
            base.Dispose();
        }

        protected override void OnScaleChanged(float oldScale)
        {
            FlushTextCache();
        }

        protected abstract void Flush();

        public void FlushTextCache()
        {
            // todo: some auto-expiring cache? based on number of elements or age
            foreach (var textRenderer in stringCache.Values)
            {
                textRenderer.Dispose();
            }
            stringCache.Clear();
        }

        public override void DrawFilledRect(Rectangle rect)
        {
            if (textureEnabled)
            {
                Flush();
                GL.Disable(EnableCap.Texture2D);
                textureEnabled = false;
            }

            rect = Translate(rect);

            DrawRect(rect);
        }

        public override Color DrawColor
        {
            get { return color; }
            set
            {
                color = value;
            }
        }

        public override void StartClip()
        {
            clipEnabled = true;
        }

        public override void EndClip()
        {
            clipEnabled = false;
        }

        public override void DrawTexturedRect(Texture t, Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            // Missing image, not loaded properly?
            if (null == t.RendererData)
            {
                DrawMissingImage(rect);
                return;
            }

            int tex = (int)t.RendererData;
            rect = Translate(rect);

            bool differentTexture = (tex != lastTextureID);
            if (!textureEnabled || differentTexture)
            {
                Flush();
            }

            if (!textureEnabled)
            {
                GL.Enable(EnableCap.Texture2D);
                textureEnabled = true;
            }

            if (differentTexture)
            {
                GL.BindTexture(TextureTarget.Texture2D, tex);
                lastTextureID = tex;
            }

            DrawRect(rect, u1, v1, u2, v2);
        }

        protected abstract void DrawRect(Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1);

        public override bool LoadFont(Font font)
        {
            font.RealSize = (float)Math.Ceiling(font.Size * Scale);
            SKFont sysFont = font.RendererData as SKFont;
            sysFont?.Dispose();

            
            SKFontStyleWeight fontStyleWeight = SKFontStyleWeight.Normal;
            if (font.Bold) fontStyleWeight = SKFontStyleWeight.Bold;
            //if (font.Italic) fontStyle |= System.Drawing.FontStyle.Italic;
            //if (font.Underline) fontStyle |= System.Drawing.FontStyle.Underline;
            //if (font.Strikeout) fontStyle |= System.Drawing.FontStyle.Strikeout;

            SKTypeface typeFace = SKTypeface.FromFamilyName(font.FaceName, fontStyleWeight, SKFontStyleWidth.Normal, font.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
            sysFont = new SKFont(typeFace, font.RealSize, Scale)
            {
                Subpixel = false,
                ForceAutoHinting = true,
                Hinting = SKFontHinting.Normal,
                Edging = SKFontEdging.Antialias,
            };


            font.RendererData = sysFont;

            return true;
        }

        public override void FreeFont(Font font)
        {
            if (font.RendererData == null)
                return;

            SKFont sysFont = font.RendererData as SKFont ?? throw new InvalidOperationException("Freeing empty font");

            sysFont.Dispose();
            font.RendererData = null;
        }

        public override FontMetrics GetFontMetrics(Font font)
        {
            SKFont sysFont = font.RendererData as SKFont;

            if (sysFont == null || Math.Abs(font.RealSize - font.Size * Scale) > 2)
            {
                FreeFont(font);
                LoadFont(font);
                sysFont = font.RendererData as SKFont;
            }

            float heightPixels = sysFont.Size * Scale;
            float ascentPixels = -sysFont.Metrics.Ascent * Scale;
            float descentPixels = sysFont.Metrics.Descent * Scale;
            float cellHeightPixels = ascentPixels + descentPixels;
            float internalLeadingPixels = cellHeightPixels - heightPixels;
            float lineSpacingPixels = sysFont.Metrics.Leading * Scale + cellHeightPixels;
            float externalLeadingPixels = lineSpacingPixels - cellHeightPixels;

            FontMetrics fm = new FontMetrics
            (
                heightPixels,
                ascentPixels,
                descentPixels,
                cellHeightPixels,
                internalLeadingPixels,
                lineSpacingPixels,
                externalLeadingPixels
            );

            return fm;
        }

        public override Size MeasureText(Font font, string text)
        {
            SKFont sysFont = font.RendererData as SKFont;

            if (sysFont == null || Math.Abs(font.RealSize - font.Size * Scale) > 2)
            {
                FreeFont(font);
                LoadFont(font);
                sysFont = font.RendererData as SKFont;
            }

            var key = new Tuple<String, Font>(text, font);

            if (stringCache.TryGetValue(key, out TextRenderer val))
            {
                Texture tex = val.Texture;
                return new Size(tex.Width, tex.Height);
            }

            float width = sysFont.MeasureText(text);
            return new Size(Util.Ceil(width + Scale), Util.Ceil(font.RealSize + 1));
        }

        public override void RenderText(Font font, Point position, string text)
        {
            Flush();

            SKFont sysFont = font.RendererData as SKFont;

            if (sysFont == null || Math.Abs(font.RealSize - font.Size * Scale) > 2)
            {
                FreeFont(font);
                LoadFont(font);
                sysFont = font.RendererData as SKFont;
            }

            var key = new Tuple<String, Font>(text, font);

            if (!stringCache.ContainsKey(key))
            {
                // not cached - create text renderer
                Size size = MeasureText(font, text);
                TextRenderer tr = new TextRenderer(size.Width, size.Height, this);

                //need to shift down, because skia seems to take the position as the intended baseline position
                tr.DrawString(text, sysFont, SKColors.White, new(0, (int)(font.RealSize - font.FontMetrics.DescentPixels))); // renders string on the texture

                DrawTexturedRect(tr.Texture, new Rectangle(position.X, position.Y, tr.Texture.Width, tr.Texture.Height));

                stringCache[key] = tr;
            }
            else
            {
                TextRenderer tr = stringCache[key];
                DrawTexturedRect(tr.Texture, new Rectangle(position.X, position.Y, tr.Texture.Width, tr.Texture.Height));
            }
        }

        internal static void LoadTextureInternal(Texture t, Bitmap bmp)
        {
            PixelFormat pixelFormat = bmp.ColorType switch
            {
                SKColorType.Rgba8888 => PixelFormat.Rgba,
                SKColorType.Bgra8888 => PixelFormat.Bgra,
                _ => throw new NotSupportedException($"Unsupported SKColorType {bmp.ColorType}")
            };

            // Create the opengl texture
            GL.GenTextures(1, out int glTex);

            GL.BindTexture(TextureTarget.Texture2D, glTex);
            lastTextureID = glTex;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Sort out our GWEN texture
            t.RendererData = glTex;
            t.Width = bmp.Width;
            t.Height = bmp.Height;

            nint data = bmp.GetPixels();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, t.Width, t.Height, 0, pixelFormat, PixelType.UnsignedByte, data);

            bmp.Dispose();
        }

        public override void LoadTexture(Texture t)
        {
            Bitmap bmp;
            try
            {
                bmp = ImageLoader.Load(t.Name);
            }
            catch (Exception e)
            {
                Console.WriteLine($"LoadTexture exception for file \"{t.Name}\": {e.Message}\n{e.StackTrace}");
                t.Failed = true;
                return;
            }

            LoadTextureInternal(t, bmp);
            bmp.Dispose();
        }

        public override void LoadTextureStream(Texture t, System.IO.Stream data)
        {
            Bitmap bmp;

            try
            {
                bmp = Bitmap.Decode(data) ?? throw new Exception("Failed to decode stream");
            }
            catch (Exception e)
            {
                Console.WriteLine($"LoadTextureStream exception: {e.Message}\n{e.StackTrace}");
                t.Failed = true;
                return;
            }

            LoadTextureInternal(t, bmp);
            bmp.Dispose();
        }

        public override void LoadTextureRaw(Texture t, byte[] pixelData)
        {
            Bitmap bmp;
            try
            {
                unsafe
                {
                    fixed (byte* ptr = &pixelData[0])
                    {
                        //bmp = new Bitmap(t.Width, t.Height, 4 * t.Width, System.Drawing.Imaging.PixelFormat.Format32bppArgb, (IntPtr)ptr);
                        bmp = new Bitmap(t.Width, t.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
                        bmp.SetPixels((IntPtr)ptr);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"LoadTextureRaw exception: {e.Message}\n{e.StackTrace}");
                t.Failed = true;
                return;
            }

            int glTex;

            // Create the opengl texture
            GL.GenTextures(1, out glTex);

            GL.BindTexture(TextureTarget.Texture2D, glTex);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Sort out our GWEN texture
            t.RendererData = glTex;

            //var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
            //    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            nint data = bmp.GetPixels();

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, t.Width, t.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            //bmp.UnlockBits(data);
            bmp.Dispose();

            //[halfofastaple] Must rebind previous texture, to ensure creating a texture doesn't mess with the render flow.
            // Setting m_LastTextureID isn't working, for some reason (even if you always rebind the texture,
            // even if the previous one was the same), we are probably making draw calls where we shouldn't be?
            // Eventually the bug needs to be fixed (color picker in a window causes graphical errors), but for now,
            // this is fine.
            GL.BindTexture(TextureTarget.Texture2D, lastTextureID);

        }

        public override void FreeTexture(Texture t)
        {
            if (t.RendererData == null)
                return;
            int tex = (int)t.RendererData;
            if (tex == 0)
                return;
            GL.DeleteTextures(1, ref tex);
            t.RendererData = null;
        }

        public override unsafe Color PixelColor(Texture texture, uint x, uint y, Color defaultColor)
        {
            if (texture.RendererData == null)
                return defaultColor;

            int tex = (int)texture.RendererData;
            if (tex == 0)
                return defaultColor;

            Color pixel;

            GL.BindTexture(TextureTarget.Texture2D, tex);
            lastTextureID = tex;

            long offset = 4 * (x + y * texture.Width);
            byte[] data = new byte[4 * texture.Width * texture.Height];
            fixed (byte* ptr = &data[0])
            {
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                pixel = new Color(data[offset + 3], data[offset + 0], data[offset + 1], data[offset + 2]);
            }

            return pixel;
        }

        public abstract void Resize(int width, int height);
    }
}