using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gwen.Net.OpenTk.Exceptions;
using SkiaSharp;

namespace Gwen.Net.OpenTk
{
    public static class ImageLoader
    {
        public delegate SKBitmap Loader(string filename);

        public static readonly Dictionary<string, Loader> loaders = new Dictionary<string, Loader>
        {
            { "jpeg", StandardLoader},
            { "jpe", StandardLoader},
            { "jfif", StandardLoader},
            { "jpg", StandardLoader},
            { "bmp", StandardLoader},
            { "dib", StandardLoader},
            { "rle", StandardLoader},
            { "png", StandardLoader},
            { "gif", StandardLoader},
            { "tif", StandardLoader},
            { "exif", StandardLoader},
            { "wmf", StandardLoader},
            { "emf", StandardLoader},
        };

        public static SKBitmap StandardLoader(string s)
        {
            return SKBitmap.Decode(s) ?? throw new FileNotFoundException(s);
        }

        public static SKBitmap Load(string filename)
        {
            string resourceType = filename.ToLower().Split('.').Last();
            if (loaders.TryGetValue(resourceType, out var loader))
            {
                return loader.Invoke(filename);
            }

            throw new ResourceLoaderNotFoundException(resourceType);
        }
    }
}