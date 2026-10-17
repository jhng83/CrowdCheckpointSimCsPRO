using System.Diagnostics;
using OpenCvSharp;
using Silk.NET.OpenGL;

namespace CrowdCheckpointSim.Recording;

/// <summary>
/// Captures the fully rendered OpenGL client framebuffer after ImGui has drawn the complete UI.
/// Recording is downstream of the simulation update and therefore does not modify simulation state,
/// physics, crowd rules, operator rules, or the fixed simulation timestep.
/// </summary>
public sealed class FullUiRecorder : IDisposable
{
    public const double TargetFramesPerSecond = 30.0;
    private const int MaxDuplicateFramesPerRender = 8;

    private readonly Stopwatch _clock = new();
    private VideoWriter? _writer;
    private Mat? _rgbaFrame;
    private Mat? _uprightFrame;
    private Mat? _bgrFrame;
    private Mat? _resizedFrame;
    private int _captureWidth;
    private int _captureHeight;
    private int _recordWidth;
    private int _recordHeight;
    private long _framesWritten;
    private bool _startRequested;
    private bool _stopRequested;
    private bool _disposed;
    private TimeSpan _lastDuration;

    public bool IsRecording => _writer is not null;
    public bool IsStarting => _startRequested && _writer is null;
    public bool IsStopping => _stopRequested && _writer is not null;
    public bool IsActive => IsRecording || IsStarting || IsStopping;
    public string? CurrentOutputPath { get; private set; }
    public string? LastSavedPath { get; private set; }
    public string? LastError { get; private set; }
    public long FramesWritten => _framesWritten;
    public TimeSpan Elapsed => IsRecording ? _clock.Elapsed : _lastDuration;

    /// <summary>Requests recording to begin on the next fully rendered application frame.</summary>
    public void RequestStart()
    {
        ThrowIfDisposed();
        if (IsActive)
        {
            return;
        }

        LastError = null;
        LastSavedPath = null;
        CurrentOutputPath = null;
        _lastDuration = TimeSpan.Zero;
        _framesWritten = 0;
        _stopRequested = false;
        _startRequested = true;
    }

    /// <summary>Requests the active recording to stop and finalize.</summary>
    public void RequestStop()
    {
        ThrowIfDisposed();
        if (!IsActive)
        {
            return;
        }

        _stopRequested = true;
    }

    /// <summary>
    /// Called once per rendered frame after ImGui.Render(). The method captures the full OpenGL
    /// framebuffer while recording is active and writes a real-time 30 fps video stream.
    /// </summary>
    public unsafe void ProcessRenderedFrame(GL gl, int framebufferWidth, int framebufferHeight)
    {
        ThrowIfDisposed();

        // Stop before capturing another frame. This prevents Reset/Back-to-Setup from being added
        // to the end of a recording when those actions also request a stop.
        if (_stopRequested)
        {
            if (_writer is not null)
            {
                StopInternal(saveAsSuccessful: true);
            }
            else
            {
                _startRequested = false;
                _stopRequested = false;
            }

            return;
        }

        if (_startRequested && _writer is null)
        {
            _startRequested = false;
            try
            {
                if (!TryStartWriter(framebufferWidth, framebufferHeight))
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                LastError = $"Could not initialize the video recorder: {exception.Message}";
                return;
            }
        }

        if (_writer is null)
        {
            return;
        }

        try
        {
            CaptureIfDue(gl, framebufferWidth, framebufferHeight);
        }
        catch (Exception exception)
        {
            LastError = $"Recording failed while capturing a frame: {exception.Message}";
            StopInternal(saveAsSuccessful: false);
        }
    }

    /// <summary>Immediately finalizes any active recording. Safe to call during shutdown.</summary>
    public void StopAndFinalize()
    {
        if (_disposed)
        {
            return;
        }

        _startRequested = false;
        _stopRequested = false;
        if (_writer is not null)
        {
            StopInternal(saveAsSuccessful: true);
        }
    }

    private bool TryStartWriter(int framebufferWidth, int framebufferHeight)
    {
        if (framebufferWidth <= 1 || framebufferHeight <= 1)
        {
            LastError = "Recording could not start because the application framebuffer has no usable size.";
            return false;
        }

        _recordWidth = RecordingPathPolicy.MakeEvenDimension(framebufferWidth);
        _recordHeight = RecordingPathPolicy.MakeEvenDimension(framebufferHeight);

        string directory;
        try
        {
            directory = RecordingPathPolicy.EnsureRecordingDirectory();
        }
        catch (Exception exception)
        {
            LastError = $"Could not create the recordings folder: {exception.Message}";
            return false;
        }

        DateTime now = DateTime.Now;
        (string Codec, string Extension)[] encoderCandidates =
        [
            ("mp4v", ".mp4"),
            ("MJPG", ".avi"),
        ];

        foreach ((string codec, string extension) in encoderCandidates)
        {
            string outputPath = Path.Combine(
                directory,
                RecordingPathPolicy.BuildFileName(now, extension));

            if (TryOpenWriter(outputPath, codec, _recordWidth, _recordHeight, out VideoWriter? writer))
            {
                _writer = writer;
                CurrentOutputPath = outputPath;
                _framesWritten = 0;
                _lastDuration = TimeSpan.Zero;
                _clock.Restart();
                LastError = null;
                return true;
            }
        }

        LastError =
            "Could not start the video encoder. The OpenCvSharp Windows runtime must be present in the application output.";
        return false;
    }

    private static bool TryOpenWriter(
        string outputPath,
        string codec,
        int width,
        int height,
        out VideoWriter? openedWriter)
    {
        openedWriter = null;

        foreach (VideoCaptureAPIs backend in new[] { VideoCaptureAPIs.FFMPEG, VideoCaptureAPIs.ANY })
        {
            VideoWriter? candidate = null;
            try
            {
                candidate = new VideoWriter();
                bool opened = candidate.Open(
                    outputPath,
                    backend,
                    FourCC.FromString(codec),
                    TargetFramesPerSecond,
                    new Size(width, height),
                    true);

                if (opened && candidate.IsOpened())
                {
                    openedWriter = candidate;
                    return true;
                }
            }
            catch
            {
                // Try the next backend/codec. One consolidated error is shown if all candidates fail.
            }

            candidate?.Dispose();
            TryDeleteFile(outputPath);
        }

        return false;
    }

    private unsafe void CaptureIfDue(GL gl, int framebufferWidth, int framebufferHeight)
    {
        if (_writer is null || framebufferWidth <= 1 || framebufferHeight <= 1)
        {
            return;
        }

        long targetFrameCount = Math.Max(
            1L,
            (long)Math.Floor(_clock.Elapsed.TotalSeconds * TargetFramesPerSecond) + 1L);
        long missingFrames = targetFrameCount - _framesWritten;
        if (missingFrames <= 0)
        {
            return;
        }

        EnsureCaptureBuffers(framebufferWidth, framebufferHeight);
        if (_rgbaFrame is null || _uprightFrame is null || _bgrFrame is null)
        {
            return;
        }

        gl.ReadPixels(
            0,
            0,
            (uint)framebufferWidth,
            (uint)framebufferHeight,
            Silk.NET.OpenGL.PixelFormat.Rgba,
            PixelType.UnsignedByte,
            _rgbaFrame.Data.ToPointer());

        // OpenGL framebuffer origin is bottom-left. Video/image origin is top-left.
        Cv2.Flip(_rgbaFrame, _uprightFrame, FlipMode.X);
        Cv2.CvtColor(_uprightFrame, _bgrFrame, ColorConversionCodes.RGBA2BGR);

        Mat frameToWrite = _bgrFrame;
        if (framebufferWidth != _recordWidth || framebufferHeight != _recordHeight)
        {
            _resizedFrame ??= new Mat();
            Cv2.Resize(
                _bgrFrame,
                _resizedFrame,
                new Size(_recordWidth, _recordHeight),
                0,
                0,
                InterpolationFlags.Area);
            frameToWrite = _resizedFrame;
        }

        int copies = (int)Math.Min(missingFrames, MaxDuplicateFramesPerRender);
        for (int index = 0; index < copies; index++)
        {
            _writer.Write(frameToWrite);
        }

        _framesWritten += copies;
    }

    private void EnsureCaptureBuffers(int width, int height)
    {
        if (_rgbaFrame is not null && _captureWidth == width && _captureHeight == height)
        {
            return;
        }

        DisposeCaptureBuffers();
        _captureWidth = width;
        _captureHeight = height;
        _rgbaFrame = new Mat(height, width, MatType.CV_8UC4);
        _uprightFrame = new Mat();
        _bgrFrame = new Mat();
        _resizedFrame = new Mat();
    }

    private void StopInternal(bool saveAsSuccessful)
    {
        _clock.Stop();
        _lastDuration = _clock.Elapsed;
        _startRequested = false;
        _stopRequested = false;

        VideoWriter? writer = _writer;
        _writer = null;
        string? outputPath = CurrentOutputPath;
        CurrentOutputPath = null;

        try
        {
            writer?.Release();
        }
        catch (Exception exception)
        {
            saveAsSuccessful = false;
            LastError ??= $"Recording could not finalize cleanly: {exception.Message}";
        }
        finally
        {
            writer?.Dispose();
        }

        if (saveAsSuccessful &&
            !string.IsNullOrWhiteSpace(outputPath) &&
            File.Exists(outputPath) &&
            new FileInfo(outputPath).Length > 0)
        {
            LastSavedPath = outputPath;
        }
        else if (!string.IsNullOrWhiteSpace(outputPath))
        {
            TryDeleteFile(outputPath);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private void DisposeCaptureBuffers()
    {
        _rgbaFrame?.Dispose();
        _uprightFrame?.Dispose();
        _bgrFrame?.Dispose();
        _resizedFrame?.Dispose();
        _rgbaFrame = null;
        _uprightFrame = null;
        _bgrFrame = null;
        _resizedFrame = null;
        _captureWidth = 0;
        _captureHeight = 0;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FullUiRecorder));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopAndFinalize();
        DisposeCaptureBuffers();
        _disposed = true;
    }
}
