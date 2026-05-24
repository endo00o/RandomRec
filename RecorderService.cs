using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using RandomRec.Resources;

namespace RandomRec
{
    /// <summary>
    /// Main orchestrator: runs the random-recording loop.
    /// </summary>
    class RecorderService
    {
        private readonly Image _previewImage;
        private readonly Random _rng = new();
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public bool IsRunning => _loopTask != null && !_loopTask.IsCompleted;

        public Action<string>? OnLog = null;
        public Action? OnRecordingStarted = null;
        public Action? OnRecordingStopped = null;

        public RecorderService(Image previewImage)
        {
            _previewImage = previewImage;
        }

        public void Start(RecorderSettings settings)
        {
            if (IsRunning) return;

            Directory.CreateDirectory(settings.OutputFolder);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _loopTask = Task.Run(() => MainLoop(settings, token), token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task MainLoop(RecorderSettings s, CancellationToken token)
        {
            OnLog?.Invoke(Strings.LogServiceStarted);

            bool isFirst = true;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!isFirst)
                    {
                        int waitMinutes = _rng.Next(s.MinIntervalMinutes, s.MaxIntervalMinutes + 1);
                        OnLog?.Invoke(string.Format(Strings.LogNextIn, waitMinutes));

                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(waitMinutes), token);
                        }
                        catch (TaskCanceledException) { break; }
                    }
                    else
                    {
                        OnLog?.Invoke(Strings.LogFirstNow);
                        isFirst = false;
                    }

                    int durationSeconds = _rng.Next(s.MinDurationSeconds, s.MaxDurationSeconds + 1);
                    await DoRecording(s, durationSeconds, token);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(string.Format(Strings.LogLoopError, ex.Message));
            }
            finally
            {
                OnLog?.Invoke(Strings.LogServiceStopped);
            }
        }

        private async Task DoRecording(RecorderSettings s, int durationSeconds, CancellationToken token)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string aviPath = Path.Combine(s.OutputFolder, $"rec_{stamp}.avi");
            string wavPath = Path.Combine(s.OutputFolder, $"rec_{stamp}.wav");
            string mp4Path = Path.Combine(s.OutputFolder, $"rec_{stamp}.mp4");
            string pngPath = Path.Combine(s.OutputFolder, $"rec_{stamp}.png");

            var video = new VideoRecorder(_previewImage);
            var audio = new AudioRecorder();

            try
            {
                video.Start(s.CameraIndex, aviPath);
                audio.Start(s.MicrophoneIndex, wavPath);

                OnLog?.Invoke(string.Format(Strings.LogRecordingStarted, durationSeconds));
                OnRecordingStarted?.Invoke();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(durationSeconds), token);
                }
                catch (TaskCanceledException) { }

                if (s.TakeScreenshots)
                {
                    try { video.SaveScreenshot(pngPath); }
                    catch (Exception ex) { OnLog?.Invoke(string.Format(Strings.LogScreenshotFailed, ex.Message)); }
                }
            }
            finally
            {
                video.Stop();
                audio.Stop();
                OnRecordingStopped?.Invoke();
            }

            await Task.Delay(300);

            try
            {
                await Muxer.MuxAsync(aviPath, wavPath, mp4Path);

                try { File.Delete(aviPath); } catch { }
                try { File.Delete(wavPath); } catch { }

                OnLog?.Invoke(string.Format(Strings.LogRecordingSaved, Path.GetFileName(mp4Path)));
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(string.Format(Strings.LogMuxingFailed, ex.Message));
            }
        }
    }
}
