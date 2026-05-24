using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace RandomRec
{
    /// <summary>
    /// Captures camera frames in a background thread and updates an Image control in the UI.
    /// </summary>
    class CameraPreview
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly System.Windows.Controls.Image _imageControl;

        public bool IsRunning => _loopTask != null && !_loopTask.IsCompleted;

        public CameraPreview(System.Windows.Controls.Image imageControl)
        {
            _imageControl = imageControl;
        }

        public void Start(int cameraIndex)
        {
            // If already running, stop first.
            Stop();

            _capture = new VideoCapture(cameraIndex);
            if (!_capture.IsOpened())
            {
                _capture.Release();
                _capture = null;
                throw new Exception($"Failed to open camera with index {cameraIndex}");
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _loopTask = Task.Run(() => CaptureLoop(token), token);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                try { _loopTask?.Wait(500); } catch { /* cancellation is fine */ }
                _cts.Dispose();
                _cts = null;
            }

            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
            _loopTask = null;
        }

        private void CaptureLoop(CancellationToken token)
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested)
            {
                if (_capture == null || !_capture.IsOpened()) break;

                if (!_capture.Read(frame) || frame.Empty())
                {
                    Thread.Sleep(50);
                    continue;
                }

                // Convert the frame to a WPF bitmap and dispatch to the UI thread.
                var bitmap = frame.ToWriteableBitmap();
                bitmap.Freeze(); // Freeze so it can safely cross thread boundaries.

                _imageControl.Dispatcher.Invoke(() =>
                {
                    _imageControl.Source = bitmap;
                });

                // ~15 fps (lighter on CPU than 30)
                Thread.Sleep(66);
            }
        }
    }
}
