using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace RandomRec
{
    /// <summary>
    /// Controls one round of the "find the recording" game:
    /// hides the main window, shows the timer + stop + hint overlays, runs the countdown,
    /// reveals hints over time, pops the goose at 0:00, then shows the game-over screen.
    /// A lost recording is deleted from disk. Restores the main window when the round ends.
    /// </summary>
    class GameSession
    {
        private readonly int _roundSeconds; // задаётся игроком на заставке

        private readonly Window _mainWindow;
        private readonly string _targetPath;

        /// <summary>Invoked when the player chooses "Играть снова" on the game-over screen.</summary>
        public Action? OnPlayAgain;

        private GameTimerWindow? _timerWin;
        private GameStopWindow? _stopWin;
        private HintWindow? _hintWin;
        private GooseWindow? _gooseWin;
        private GameOverWindow? _gameOverWin;

        private DispatcherTimer? _timer;
        private DispatcherTimer? _gooseToGameOverTimer;
        private int _remaining;
        private bool _ended;
        private bool _fileDeleted;

        private List<string> _hints = new();
        private int _hintIndex;

        public GameSession(Window mainWindow, string targetHiddenPath, int roundSeconds)
        {
            _mainWindow = mainWindow;
            _roundSeconds = roundSeconds > 0 ? roundSeconds : 120;
            try { _targetPath = Path.GetFullPath(targetHiddenPath); }
            catch { _targetPath = targetHiddenPath; }
        }

        public void Start()
        {
            _mainWindow.Hide();

            _remaining = _roundSeconds;

            _timerWin = new GameTimerWindow();
            _timerWin.FilesDropped = OnFilesDropped;
            _timerWin.SetTime(_remaining);
            _timerWin.Show();
            PositionTopCenter(_timerWin);

            _stopWin = new GameStopWindow();
            _stopWin.StopRequested = () => EndGame(false);
            _stopWin.Show();
            PositionBottomRight(_stopWin);

            _hints = BuildHints(_targetPath);
            _hintIndex = 0;
            _hintWin = new HintWindow();
            _hintWin.Show();
            PositionTopLeft(_hintWin);
            if (_hints.Count > 0)
                _hintWin.SetHint(_hints[0], animate: false);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _remaining--;

            if (_remaining <= 0)
            {
                _remaining = 0;
                _timerWin?.SetTime(0);
                _timer?.Stop();
                OnTimeout();
                return;
            }

            _timerWin?.SetTime(_remaining);

            int elapsed = _roundSeconds - _remaining;
            int step = _roundSeconds / Math.Max(_hints.Count, 1);
            if (step > 0)
            {
                int targetIndex = elapsed / step;
                if (targetIndex > _hintIndex && targetIndex < _hints.Count)
                {
                    _hintIndex = targetIndex;
                    _hintWin?.SetHint(_hints[_hintIndex]);
                }
            }
        }

        private void OnTimeout()
        {
            if (_ended) return;

            // Запись не найдена — удаляем её навсегда.
            DeleteHiddenFile();

            // Гусь "Toasty!"
            _gooseWin = new GooseWindow();
            _gooseWin.Show();
            PositionGoose(_gooseWin);

            // После того как гусь уехал — показываем экран проигрыша.
            _gooseToGameOverTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.0) };
            _gooseToGameOverTimer.Tick += (s, e) =>
            {
                _gooseToGameOverTimer?.Stop();
                _gooseToGameOverTimer = null;
                ShowGameOver();
            };
            _gooseToGameOverTimer.Start();
        }

        private void ShowGameOver()
        {
            if (_ended) return;

            // Прячем игровой HUD, оставляя только экран проигрыша.
            _timerWin?.Close(); _timerWin = null;
            _stopWin?.Close(); _stopWin = null;
            _hintWin?.Close(); _hintWin = null;

            _gameOverWin = new GameOverWindow();
            _gameOverWin.PlayAgainRequested = () => EndGame(false, startNewRound: true);
            _gameOverWin.BackToMainRequested = () => EndGame(false);
            _gameOverWin.Show();
        }

        private void OnFilesDropped(string[] files)
        {
            if (_ended || files == null) return;

            foreach (var f in files)
            {
                string dropped;
                try { dropped = Path.GetFullPath(f); }
                catch { continue; }

                if (string.Equals(dropped, _targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    EndGame(true);
                    return;
                }
            }

            _timerWin?.ShowWrong();
        }

        private void EndGame(bool won, bool startNewRound = false)
        {
            if (_ended) return;
            _ended = true;

            _timer?.Stop();
            _timer = null;
            _gooseToGameOverTimer?.Stop();
            _gooseToGameOverTimer = null;

            // Проигрыш (сдался или не успел) — запись удаляется.
            if (!won) DeleteHiddenFile();

            _timerWin?.Close();
            _stopWin?.Close();
            _hintWin?.Close();
            _gooseWin?.Close();
            _gameOverWin?.Close();
            _timerWin = null;
            _stopWin = null;
            _hintWin = null;
            _gooseWin = null;
            _gameOverWin = null;

            _mainWindow.Show();
            _mainWindow.Activate();

            if (startNewRound)
                OnPlayAgain?.Invoke();
        }

        private void DeleteHiddenFile()
        {
            if (_fileDeleted) return;
            _fileDeleted = true;
            try
            {
                if (File.Exists(_targetPath))
                    File.Delete(_targetPath);
            }
            catch { /* файл мог быть занят/перемещён — не критично */ }
        }

        // ===== Подсказки из пути (от общей к конкретной) =====

        private static List<string> BuildHints(string fullPath)
        {
            var hints = new List<string>();
            try
            {
                string root = Path.GetPathRoot(fullPath) ?? "C:\\";
                string drive = root.TrimEnd('\\', '/');
                hints.Add(GameText.HintDrive(drive));

                string lower = fullPath.ToLowerInvariant();
                string zone =
                    lower.Contains("\\appdata\\local\\temp") ? GameText.ZoneTemp :
                    lower.Contains("\\appdata\\roaming") ? GameText.ZoneRoaming :
                    lower.Contains("\\appdata\\local") ? GameText.ZoneLocal :
                    lower.Contains("\\documents") ? GameText.ZoneDocuments :
                    lower.Contains("\\videos") ? GameText.ZoneVideos :
                    lower.Contains("\\music") ? GameText.ZoneMusic :
                    lower.Contains("\\pictures") ? GameText.ZonePictures :
                                                               GameText.ZoneHome;
                hints.Add(zone);

                string? dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    string folderName = new DirectoryInfo(dir).Name;
                    if (!string.IsNullOrEmpty(folderName))
                        hints.Add(GameText.HintFolder(folderName));

                    hints.Add(GameText.HintExact(dir));
                }
            }
            catch
            {
                if (hints.Count == 0)
                    hints.Add(GameText.HintFallback);
            }
            return hints;
        }

        // ===== Позиционирование оверлеев =====

        private static void PositionTopCenter(Window w)
        {
            var area = SystemParameters.WorkArea;
            w.Left = area.Left + (area.Width - w.Width) / 2;
            w.Top = area.Top + 24;
        }

        private static void PositionTopLeft(Window w)
        {
            var area = SystemParameters.WorkArea;
            w.Left = area.Left + 24;
            w.Top = area.Top + 24;
        }

        private static void PositionBottomRight(Window w)
        {
            var area = SystemParameters.WorkArea;
            w.Left = area.Left + area.Width - w.Width - 24;
            w.Top = area.Top + area.Height - w.Height - 24;
        }

        // Гусь — вплотную к левому-нижнему краю экрана, поверх панели задач.
        private static void PositionGoose(Window w)
        {
            w.Left = 0;
            w.Top = SystemParameters.PrimaryScreenHeight - w.Height;
        }
    }
}
