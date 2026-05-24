using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace RandomRec
{
    /// <summary>
    /// Records video from a camera into an AVI file and updates a live preview.
    /// One instance per recording.
    /// </summary>
    class VideoRecorder
    {
        private VideoCapture? _capture;
        private VideoWriter? _writer;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly Image _previewImage;

        // Recording parameters
        private const double Fps = 25.0;
        private const int FrameWidth = 1280;
        private const int FrameHeight = 720;

        public bool IsRecording { get; private set; }

        // Last captured frame (for optional screenshot)
        private Mat? _lastFrame;
        private readonly object _screenshotLock = new();

        public VideoRecorder(Image previewImage)
        {
            _previewImage = previewImage;
        }

        public void Start(int cameraIndex, string outputAviPath)
        {
            if (IsRecording)
                throw new InvalidOperationException("Recording is already in progress");

            _capture = new VideoCapture(cameraIndex);
            if (!_capture.IsOpened())
            {
                _capture.Release();
                _capture = null;
                throw new Exception($"Failed to open camera {cameraIndex}");
            }

            _capture.Set(VideoCaptureProperties.FrameWidth, FrameWidth);
            _capture.Set(VideoCaptureProperties.FrameHeight, FrameHeight);

            int actualWidth = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
            int actualHeight = (int)_capture.Get(VideoCaptureProperties.FrameHeight);

            var fourcc = VideoWriter.FourCC('M', 'J', 'P', 'G');
            _writer = new VideoWriter(outputAviPath, fourcc, Fps, new Size(actualWidth, actualHeight));
            if (!_writer.IsOpened())
            {
                _capture.Release();
                _capture = null;
                throw new Exception("Failed to create video file");
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _loopTask = Task.Run(() => CaptureLoop(token), token);

            IsRecording = true;
        }

        public void Stop()
        {
            if (!IsRecording) return;

            _cts?.Cancel();
            try { _loopTask?.Wait(1000); } catch { }

            _writer?.Release();
            _writer?.Dispose();
            _writer = null;

            _capture?.Release();
            _capture?.Dispose();
            _capture = null;

            _cts?.Dispose();
            _cts = null;
            _loopTask = null;

            lock (_screenshotLock)
            {
                _lastFrame?.Dispose();
                _lastFrame = null;
            }

            IsRecording = false;
        }

        /// <summary>
        /// Saves the most recent captured frame as a PNG. Can be called at any moment during recording.
        /// </summary>
        public void SaveScreenshot(string outputPngPath)
        {
            Mat? snapshot = null;
            try
            {
                lock (_screenshotLock)
                {
                    if (_lastFrame == null || _lastFrame.IsDisposed || _lastFrame.Empty()) return;
                    snapshot = _lastFrame.Clone();
                }
                Cv2.ImWrite(outputPngPath, snapshot);
            }
            finally
            {
                snapshot?.Dispose();
            }
        }

        private void CaptureLoop(CancellationToken token)
        {
            var frame = new Mat();
            int frameInterval = (int)(1000.0 / Fps);

            while (!token.IsCancellationRequested)
            {
                if (_capture == null || !_capture.IsOpened()) break;

                if (!_capture.Read(frame) || frame.Empty())
                {
                    Thread.Sleep(10);
                    continue;
                }

                _writer?.Write(frame);

                // Update "last frame" for screenshots
                lock (_screenshotLock)
                {
                    _lastFrame?.Dispose();
                    _lastFrame = frame.Clone();
                }

                // Update preview in UI
                var bitmap = frame.ToWriteableBitmap();
                bitmap.Freeze();
                _previewImage.Dispatcher.Invoke(() =>
                {
                    _previewImage.Source = bitmap;
                });

                Thread.Sleep(frameInterval);
            }

            frame.Dispose();
        }
    }
}
