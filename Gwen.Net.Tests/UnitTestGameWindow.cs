using System;
using System.Linq;
using Collections.Generic;
using Gwen.Net.Control;
using Gwen.Net.OpenTk;
using Gwen.Net.OpenTk.Input;
using Gwen.Net.OpenTk.Platform;
using Gwen.Net.Skin;
using Gwen.Net.Tests.Components;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gwen.Net.Tests
{
    public class UnitTestGameWindow : GameWindow
    {
        private const int MaxFrameSampleSize = 10000;

        private UnitTestHarnessControls unitTestControls;

        private readonly CircularBuffer<double> updateFrameTimes;
        private readonly CircularBuffer<double> renderFrameTimes;
        private readonly IGwenGui gui;

        public UnitTestGameWindow()
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 30;

            gui = GwenGuiFactory.CreateFromGame(this, GwenGuiSettings.Default.From((settings) => 
            {
                //Have the skin come from somewhere else.
                settings.SkinFile = new System.IO.FileInfo("DefaultSkin.png");
                settings.DefaultFontSize = 14;
            }));

            updateFrameTimes = new CircularBuffer<double>(MaxFrameSampleSize);
            renderFrameTimes = new CircularBuffer<double>(MaxFrameSampleSize);
        }

        protected override void Dispose(bool disposing)
        {
            gui.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnLoad()
        {
            GL.ClearColor(Color4.MidnightBlue);
            gui.Load();
            unitTestControls = new UnitTestHarnessControls(gui.Root);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            gui.Resize(e.Size);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (UpdateFrequency == 0)
            {
                updateFrameTimes.Put(e.Time);
                if (updateFrameTimes.Sum(t => t) >= 1)
                {
                    int frames = updateFrameTimes.Count();
                    updateFrameTimes.Clear();
                    unitTestControls.UpdateFps = frames;
                }
            }
            else
            {
                unitTestControls.UpdateFps = UpdateFrequency;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
            gui.Render();
            SwapBuffers();

            if (UpdateFrequency == 0)
            {
                renderFrameTimes.Put(e.Time);
                if (renderFrameTimes.Sum(t => t) >= 1)
                {
                    int frames = renderFrameTimes.Count();
                    renderFrameTimes.Clear();
                    unitTestControls.RenderFps = frames;
                }
            }
            else
            {
                unitTestControls.RenderFps = UpdateFrequency;
            }
        }

        [STAThread]
        public static void Main()
        {
            using (UnitTestGameWindow window = new UnitTestGameWindow())
            {
                window.Title = "Gwen.net OpenTK Unit Test";
                window.VSync = VSyncMode.Off; // to measure performance
                window.Run();
            }
        }
    }
}