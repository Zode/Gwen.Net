using System.ComponentModel;
using OpenTK.Windowing.Desktop;

namespace Gwen.Net.OpenTk
{
    public static class GwenGuiFactory
    {
        public static IGwenGui CreateFromGame(GameWindow window, GwenGuiSettings settings = default)
        {
            if (settings == null)
            {
                settings = GwenGuiSettings.Default;
            }

            return new GwenGui(window, settings);
        }
    }
}
