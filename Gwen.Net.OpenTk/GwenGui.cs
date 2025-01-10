using Gwen.Net.Control;
using Gwen.Net.OpenTk.Exceptions;
using Gwen.Net.OpenTk.Input;
using Gwen.Net.OpenTk.Platform;
using Gwen.Net.OpenTk.Renderers;
using Gwen.Net.Platform;
using Gwen.Net.Skin;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;

namespace Gwen.Net.OpenTk
{
    internal class GwenGui : IGwenGui
    {
        private OpenTKRendererBase renderer;
        private SkinBase skin;
        private Canvas canvas;
        private OpenTkInputTranslator input;

        public GwenGuiSettings Settings { get; }

        public GameWindow Parent { get; }

        public ControlBase Root => canvas;

        internal GwenGui(GameWindow parent, GwenGuiSettings settings)
        {
            Parent = parent;
            Settings = settings;
        }

        public void Load()
        {
            GwenPlatform.Init(new NetCorePlatform(SetCursor));
            AttachToWindowEvents();
            renderer = ResolveRenderer(Settings.Renderer);
            skin = new TexturedBase(renderer, "DefaultSkin2.png")
            {
                DefaultFont = new Font(renderer, "Calibri", 11)
            };
            canvas = new Canvas(skin);
            input = new OpenTkInputTranslator(canvas);

            canvas.SetSize(Parent.Size.X, Parent.Size.Y);
            canvas.ShouldDrawBackground = true;
            canvas.BackgroundColor = skin.Colors.ModalBackground;
        }

        public void Render()
        {
            canvas.RenderCanvas();
        }

        public void Resize(Vector2i size)
        {
            renderer.Resize(size.X, size.Y);
            canvas.SetSize(size.X, size.Y);
        }

        public void Dispose()
        {
            DetachWindowEvents();
            canvas.Dispose();
            skin.Dispose();
            renderer.Dispose();
        }

        private void AttachToWindowEvents()
        {
            Parent.KeyUp += Parent_KeyUp;
            Parent.KeyDown += Parent_KeyDown;
            Parent.TextInput += Parent_TextInput;
            Parent.MouseDown += Parent_MouseDown;
            Parent.MouseUp += Parent_MouseUp;
            Parent.MouseMove += Parent_MouseMove;
            Parent.MouseWheel += Parent_MouseWheel;
        }

        private void DetachWindowEvents()
        {
            Parent.KeyUp -= Parent_KeyUp;
            Parent.KeyDown -= Parent_KeyDown;
            Parent.TextInput -= Parent_TextInput;
            Parent.MouseDown -= Parent_MouseDown;
            Parent.MouseUp -= Parent_MouseUp;
            Parent.MouseMove -= Parent_MouseMove;
            Parent.MouseWheel -= Parent_MouseWheel;
        }

        private void Parent_KeyUp(KeyboardKeyEventArgs obj)
            => input.ProcessKeyUp(obj);

        private void Parent_KeyDown(KeyboardKeyEventArgs obj)
            => input.ProcessKeyDown(obj);

        private void Parent_TextInput(TextInputEventArgs obj)
            => input.ProcessTextInput(obj);

        private void Parent_MouseDown(MouseButtonEventArgs obj)
            => input.ProcessMouseButton(obj);

        private void Parent_MouseUp(MouseButtonEventArgs obj)
            => input.ProcessMouseButton(obj);

        private void Parent_MouseMove(MouseMoveEventArgs obj)
            => input.ProcessMouseMove(obj);

        private void Parent_MouseWheel(MouseWheelEventArgs obj)
            => input.ProcessMouseWheel(obj);

        private void SetCursor(MouseCursor mouseCursor)
        {
            Parent.Cursor = mouseCursor;
        }

        private static OpenTKRendererBase ResolveRenderer(GwenGuiRenderer gwenGuiRenderer)
        {
            switch (gwenGuiRenderer)
            {
                case GwenGuiRenderer.GL10:
                    return new OpenTKGL10Renderer();
                case GwenGuiRenderer.GL20:
                    return new OpenTKGL20Renderer();
                case GwenGuiRenderer.GL40:
                    return new OpenTKGL40Renderer();
                default:
                    throw new RendererNotFoundException(gwenGuiRenderer);
            };
        }
    }
}
