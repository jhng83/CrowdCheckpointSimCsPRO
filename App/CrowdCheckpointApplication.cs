using System.Numerics;
using CrowdCheckpointSim.Core;
using CrowdCheckpointSim.Recording;
using CrowdCheckpointSim.UI;
using CrowdCheckpointSim.Visual;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace CrowdCheckpointSim.App;

/// <summary>Creates the native window, OpenGL context, ImGui backend, and main application loop.</summary>
public sealed class CrowdCheckpointApplication : IDisposable
{
    private readonly IWindow _window;
    private GL? _gl;
    private IInputContext? _input;
    private ImGuiController? _imgui;
    private SimulationUi? _ui;
    private FullUiRecorder? _recorder;
    private bool _disposed;
    private bool _graphicsDisposed;
    private int _viewportWidth = -1;
    private int _viewportHeight = -1;

    private CrowdCheckpointApplication()
    {
        WindowOptions options = WindowOptions.Default;
        options.Title = "Crowd Operations Simulator";
        options.Size = new Vector2D<int>(1500, 900);
        options.WindowState = WindowState.Maximized;
        options.VSync = true;
        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnClosing;
    }

    /// <summary>Application entry point used by Program.cs.</summary>
    public static int Run()
    {
        using CrowdCheckpointApplication application = new();
        application._window.Run();
        return 0;
    }

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();
        SynchronizeViewport(_window.FramebufferSize);
        _input = _window.CreateInput();
        _imgui = new ImGuiController(_gl, _window, _input, ProductTheme.ConfigureFonts);

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        ProductTheme.Apply();

        SimulationConfig config = new();
        CrowdSimulation simulation = new(config);
        _recorder = new FullUiRecorder();
        _ui = new SimulationUi(simulation, _recorder);
    }

    private void OnRender(double deltaSeconds)
    {
        if (_gl is null || _imgui is null || _ui is null)
        {
            return;
        }

        _ui.Advance((float)deltaSeconds);
        _imgui.Update((float)deltaSeconds);
        SynchronizeViewport(_window.FramebufferSize);

        _gl.ClearColor(
            ProfessionalPalette.AppBackground.X,
            ProfessionalPalette.AppBackground.Y,
            ProfessionalPalette.AppBackground.Z,
            ProfessionalPalette.AppBackground.W);
        _gl.Clear((uint)ClearBufferMask.ColorBufferBit);

        Vector2D<int> size = _window.Size;
        _ui.Draw(new Vector2(size.X, size.Y));
        _imgui.Render();

        Vector2D<int> framebufferSize = _window.FramebufferSize;
        _recorder?.ProcessRenderedFrame(_gl, framebufferSize.X, framebufferSize.Y);

        if (_ui.ExitRequested)
        {
            _window.Close();
        }
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        SynchronizeViewport(size);
    }

    /// <summary>
    /// Keep the OpenGL viewport aligned with the real framebuffer. A maximized window can receive
    /// its first resize event before the OpenGL context exists, so relying on the event alone leaves
    /// the initial 1500 x 900 viewport rendered inside a larger client area.
    /// </summary>
    private void SynchronizeViewport(Vector2D<int> size)
    {
        if (_gl is null || size.X <= 0 || size.Y <= 0 ||
            (size.X == _viewportWidth && size.Y == _viewportHeight))
        {
            return;
        }

        _gl.Viewport(size);
        _viewportWidth = size.X;
        _viewportHeight = size.Y;
    }

    private void OnClosing()
    {
        DisposeGraphicsResources();
    }

    private void DisposeGraphicsResources()
    {
        if (_graphicsDisposed)
        {
            return;
        }

        _graphicsDisposed = true;
        _recorder?.StopAndFinalize();
        _recorder?.Dispose();
        _recorder = null;
        _imgui?.Dispose();
        _input?.Dispose();
        _gl?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeGraphicsResources();
        _window.Dispose();
    }
}
