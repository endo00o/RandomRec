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

        // Игровой режим: вызывается после того, как запись спрятана.
        // Передаёт полный путь к спрятанному файлу (нужен окну игры для проверки находки).
        public Action<string>? OnRecordingHidden = null;

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

                    // В игровом режиме делаем ОДНУ запись и выходим из цикла —
                    // дальше начинается игра «найди файл».
                    if (s.GameMode) break;
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
            string mp4Path = Path.Combine(s.OutputFolder, $"rec_{stamp}.mp4");
            string pngPath = Path.Combine(s.OutputFolder, $"rec_{stamp}.png");

            // Для скриншота нужен отдельный OpenCV-захват, потому что ffmpeg держит камеру
            // эксклюзивно. Скриншот сделаем ДО ffmpeg, в самом начале.
            if (s.TakeScreenshots)
            {
                try
                {
                    SaveQuickScreenshot(s.CameraIndex, pngPath);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke(string.Format(Strings.LogScreenshotFailed, ex.Message));
                }
            }

            var recorder = new FfmpegRecorder();

            try
            {
                recorder.Start(s.CameraName, s.MicrophoneName, mp4Path);

                OnLog?.Invoke(string.Format(Strings.LogRecordingStarted, durationSeconds));
                OnRecordingStarted?.Invoke();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(durationSeconds), token);
                }
                catch (TaskCanceledException) { }
            }
            finally
            {
                await recorder.StopAsync();
                OnRecordingStopped?.Invoke();
            }

            // Файл готов. В игровом режиме — прячем его, иначе просто сообщаем о сохранении.
            if (s.GameMode)
            {
                try
                {
                    string hiddenPath = HideRecording(mp4Path);
                    // ВРЕМЕННО (для отладки этапа 1): показываем, куда спряталось.
                    // В финальной игре этот лог нужно убрать — иначе это спойлер для игрока.
                    OnLog?.Invoke($"[GAME] Запись спрятана: {hiddenPath}");
                    OnRecordingHidden?.Invoke(hiddenPath);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[GAME] Не удалось спрятать запись: {ex.Message}");
                }
            }
            else
            {
                OnLog?.Invoke(string.Format(Strings.LogRecordingSaved, Path.GetFileName(mp4Path)));
            }
        }

        // ===== Игровой режим: прятки файла =====

        // Папки-укрытия, куда может уехать запись.
        private static readonly string[] HideRoots =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),       // %APPDATA% (Roaming)
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),  // %LOCALAPPDATA%
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Path.GetTempPath(),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        // Неприметные имена подпапок, чтобы файл было не так-то просто найти.
        private static readonly string[] DecoyFolderNames =
        {
            "cache", "data", "tmp", "backup", "logs", "store", ".config"
        };

        /// <summary>
        /// Moves the finished recording into a random "hidden" folder somewhere on the
        /// user's machine and returns the new full path. Used by the game mode.
        /// </summary>
        private string HideRecording(string sourceMp4Path)
        {
            string root = HideRoots[_rng.Next(HideRoots.Length)];

            // Защита на случай, если какая-то из системных папок недоступна.
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string decoy = DecoyFolderNames[_rng.Next(DecoyFolderNames.Length)];

            string hideDir = Path.Combine(root, decoy);
            Directory.CreateDirectory(hideDir);

            string destPath = Path.Combine(hideDir, Path.GetFileName(sourceMp4Path));
            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Move(sourceMp4Path, destPath);
            return destPath;
        }

        /// <summary>
        /// Captures one frame from the camera and saves it as PNG.
        /// Uses OpenCV temporarily — ffmpeg can't take a screenshot while another process holds the camera.
        /// </summary>
        private void SaveQuickScreenshot(int cameraIndex, string pngPath)
        {
            using var cap = new OpenCvSharp.VideoCapture(cameraIndex);
            if (!cap.IsOpened()) return;

            using var frame = new OpenCvSharp.Mat();
            // Прогрев: первые 2-3 кадра бывают чёрные
            for (int i = 0; i < 3; i++)
            {
                cap.Read(frame);
            }

            if (!frame.Empty())
            {
                OpenCvSharp.Cv2.ImWrite(pngPath, frame);
            }
        }
    }
}