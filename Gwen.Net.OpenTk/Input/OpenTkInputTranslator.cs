using System;
using Gwen.Net.Control;
using Gwen.Net.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gwen.Net.OpenTk.Input
{
    public class OpenTkInputTranslator
    {
        private readonly Canvas canvas;
        private Vector2 lastMousePosition;

        bool controlPressed = false;

        public OpenTkInputTranslator(Canvas canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        private GwenMappedKey TranslateKeyCode(Keys key)
        {
            switch (key)
            {
                case Keys.Backspace: return GwenMappedKey.Backspace;
                case Keys.Enter: return GwenMappedKey.Return;
                case Keys.KeyPadEnter: return GwenMappedKey.Return;
                case Keys.Escape: return GwenMappedKey.Escape;
                case Keys.Tab: return GwenMappedKey.Tab;
                case Keys.Space: return GwenMappedKey.Space;
                case Keys.Up: return GwenMappedKey.Up;
                case Keys.Down: return GwenMappedKey.Down;
                case Keys.Left: return GwenMappedKey.Left;
                case Keys.Right: return GwenMappedKey.Right;
                case Keys.Home: return GwenMappedKey.Home;
                case Keys.End: return GwenMappedKey.End;
                case Keys.Delete: return GwenMappedKey.Delete;
                case Keys.RightControl:
                case Keys.LeftControl:
                    controlPressed = true;
                    return GwenMappedKey.Control;
                case Keys.LeftAlt:
                case Keys.RightAlt:
                    return GwenMappedKey.Alt;
                case Keys.LeftShift:
                case Keys.RightShift: return GwenMappedKey.Shift;

            }
            return GwenMappedKey.Invalid;
        }

        private static char TranslateChar(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
                return (char)('a' + ((int)key - (int)Keys.A));
            return ' ';
        }

        public void ProcessMouseButton(MouseButtonEventArgs args)
        {
            if (canvas is null)
                return;

            if (args.Button == MouseButton.Left)
                canvas.Input_MouseButton(0, args.IsPressed);
            else if (args.Button == MouseButton.Right)
                canvas.Input_MouseButton(1, args.IsPressed);
        }

        public void ProcessMouseMove(MouseMoveEventArgs args)
        {
            if (null == canvas)
                return;

            var deltaPosition = args.Position - lastMousePosition;
            lastMousePosition = args.Position;

            canvas.Input_MouseMoved((int)lastMousePosition.X, (int)lastMousePosition.Y, (int)deltaPosition.X, (int)deltaPosition.Y);
        }

        public void ProcessMouseWheel(MouseWheelEventArgs args)
        {
            if (null == canvas)
                return;

            canvas.Input_MouseWheel((int)(args.OffsetY * 60));
        }

        public bool ProcessKeyDown(KeyboardKeyEventArgs eventArgs)
        {
            char ch = TranslateChar(eventArgs.Key);

            if (InputHandler.DoSpecialKeys(canvas, ch))
                return false;

            GwenMappedKey iKey = TranslateKeyCode(eventArgs.Key);
            if (iKey == GwenMappedKey.Invalid)
            {
                return false;
            }

            return canvas.Input_Key(iKey, true);
        }

        public void ProcessTextInput(TextInputEventArgs obj)
        {
            foreach (char c in obj.AsString)
            {
                canvas.Input_Character(c);
            }
        }

        public bool ProcessKeyUp(KeyboardKeyEventArgs eventArgs)
        {
            GwenMappedKey key = TranslateKeyCode(eventArgs.Key);

            return canvas.Input_Key(key, false);
        }
    }
}