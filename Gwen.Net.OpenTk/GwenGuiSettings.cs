using System.IO;
#nullable enable

namespace Gwen.Net.OpenTk
{
    public class GwenGuiSettings
    {
        public static readonly GwenGuiSettings Default = new GwenGuiSettings
        {
            SkinFile = null,
            DefaultFont = "Calibri",
            DefaultFontSize = 11,
            Renderer = GwenGuiRenderer.GL40,
            DrawBackground = true
        };

        //Make this a source or stream?
        public FileInfo? SkinFile { get; set; } = null;

        public string DefaultFont { get; set; } = string.Empty;
        public int DefaultFontSize { get; set; }

        public GwenGuiRenderer Renderer { get; set; }

        public bool DrawBackground { get; set; }

        private GwenGuiSettings() { }
    }
}
