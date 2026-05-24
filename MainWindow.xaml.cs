using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using NAudio.Wave;
using VideoCapture = OpenCvSharp.VideoCapture;
using DirectShowLib;
using RandomRec.Resources;

namespace RandomRec
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraPreview? _preview;
        private RecorderService? _recorder;
        private bool _isInitializing = true;

        private static readonly List<(string Code, string Display)> SupportedLanguages = new()
        {
            ("",   "Auto"),
            ("en", "English"),
            ("ru", "Русский"),
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadDevices();
            LoadSettingsToUI();
            InitLanguageComboBox();

            _preview = new CameraPreview(PreviewImage);

            _recorder = new RecorderService(PreviewImage);
            _recorder.OnLog = msg => Dispatcher.Invoke(() => Log(msg));
            _recorder.OnRecordingStarted = () => Dispatcher.Invoke(() =>
            {
                RecIndicator.Visibility = Visibility.Visible;
                _preview?.Stop();
            });
            _recorder.OnRecordingStopped = () => Dispatcher.Invoke(() =>
            {
                RecIndicator.Visibility = Visibility.Collapsed;
                StartPreview();
            });

            CameraComboBox.SelectionChanged += CameraComboBox_SelectionChanged;
            Closed += MainWindow_Closed;
            StateChanged += (s, e) => UpdateMaximizeButtonIcon();

            StartPreview();

            _isInitializing = false;
        }

        private void LoadSettingsToUI()
        {
            var s = App.Settings;

            FolderTextBox.Text = s.OutputFolder;
            MinIntervalTextBox.Text = s.MinIntervalMinutes.ToString();
            MaxIntervalTextBox.Text = s.MaxIntervalMinutes.ToString();
            MinDurationTextBox.Text = s.MinDurationSeconds.ToString();
            MaxDurationTextBox.Text = s.MaxDurationSeconds.ToString();
            ScreenshotsCheckBox.IsChecked = s.TakeScreenshots;
            SilentModeCheckBox.IsChecked = s.SilentMode;

            if (s.CameraIndex >= 0 && s.CameraIndex < CameraComboBox.Items.Count)
                CameraComboBox.SelectedIndex = s.CameraIndex;

            if (s.MicrophoneIndex >= 0 && s.MicrophoneIndex < MicrophoneComboBox.Items.Count)
                MicrophoneComboBox.SelectedIndex = s.MicrophoneIndex;
        }

        private void SaveSettingsFromUI()
        {
            var s = App.Settings;

            s.OutputFolder = FolderTextBox.Text;

            if (int.TryParse(MinIntervalTextBox.Text, out int minInt)) s.MinIntervalMinutes = minInt;
            if (int.TryParse(MaxIntervalTextBox.Text, out int maxInt)) s.MaxIntervalMinutes = maxInt;
            if (int.TryParse(MinDurationTextBox.Text, out int minDur)) s.MinDurationSeconds = minDur;
            if (int.TryParse(MaxDurationTextBox.Text, out int maxDur)) s.MaxDurationSeconds = maxDur;

            s.CameraIndex = CameraComboBox.SelectedIndex;
            s.MicrophoneIndex = MicrophoneComboBox.SelectedIndex;
            s.TakeScreenshots = ScreenshotsCheckBox.IsChecked == true;
            s.SilentMode = SilentModeCheckBox.IsChecked == true;

            s.Save();
        }

        private void InitLanguageComboBox()
        {
            foreach (var lang in SupportedLanguages)
            {
                LanguageComboBox.Items.Add(lang.Display);
            }

            string current = App.Settings.Language;
            int index = SupportedLanguages.FindIndex(l => l.Code == current);
            LanguageComboBox.SelectedIndex = index >= 0 ? index : 0;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            int idx = LanguageComboBox.SelectedIndex;
            if (idx < 0 || idx >= SupportedLanguages.Count) return;

            string newCode = SupportedLanguages[idx].Code;
            if (newCode == App.Settings.Language) return;

            SaveSettingsFromUI();

            App.Settings.Language = newCode;
            App.Settings.Save();

            App.ApplyLanguage(newCode);

            var newWindow = new MainWindow();
            Application.Current.MainWindow = newWindow;
            newWindow.Show();
            Close();
        }

        private void LoadDevices()
        {
            int micCount = WaveInEvent.DeviceCount;
            for (int i = 0; i < micCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                MicrophoneComboBox.Items.Add($"{i}: {caps.ProductName}");
            }
            if (micCount > 0) MicrophoneComboBox.SelectedIndex = 0;
            Log(string.Format(Strings.LogMicrophonesFound, micCount));

            var videoDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for (int i = 0; i < videoDevices.Length; i++)
            {
                CameraComboBox.Items.Add($"{i}: {videoDevices[i].Name}");
            }
            if (videoDevices.Length > 0) CameraComboBox.SelectedIndex = 0;
            Log(string.Format(Strings.LogCamerasFound, videoDevices.Length));
        }

        private void StartPreview()
        {
            if (CameraComboBox.SelectedItem == null) return;

            int cameraIndex = ParseIndex(CameraComboBox.SelectedItem.ToString());
            if (cameraIndex < 0) return;

            try
            {
                _preview?.Start(cameraIndex);
                PreviewPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Log(string.Format(Strings.LogPreviewError, ex.Message));
            }
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_recorder?.IsRunning == true) return;
            StartPreview();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            SaveSettingsFromUI();
            _recorder?.Stop();
            _preview?.Stop();
        }

        // ===== Кнопки кастомного титулбара =====

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == System.Windows.WindowState.Maximized
                ? System.Windows.WindowState.Normal
                : System.Windows.WindowState.Maximized;
            UpdateMaximizeButtonIcon();
        }
        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = this };
            about.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UpdateMaximizeButtonIcon()
        {
            MaximizeButton.Content = this.WindowState == System.Windows.WindowState.Maximized ? "\uE923" : "\uE922";
        }

        // ===== Остальное =====

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = Strings.BrowseDialogTitle,
                InitialDirectory = FolderTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                FolderTextBox.Text = dialog.FolderName;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildSettings(out var settings, out string error))
            {
                Log(string.Format(Strings.LogInvalidSettings, error));
                return;
            }

            _recorder?.Start(settings);

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _recorder?.Stop();

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private bool TryBuildSettings(out RecorderSettings settings, out string error)
        {
            settings = new RecorderSettings();
            error = "";

            if (string.IsNullOrWhiteSpace(FolderTextBox.Text))
            {
                error = Strings.ErrFolderNotSet;
                return false;
            }
            settings.OutputFolder = FolderTextBox.Text;

            if (CameraComboBox.SelectedItem == null)
            {
                error = Strings.ErrCameraNotSelected;
                return false;
            }
            settings.CameraIndex = ParseIndex(CameraComboBox.SelectedItem.ToString());

            if (MicrophoneComboBox.SelectedItem == null)
            {
                error = Strings.ErrMicNotSelected;
                return false;
            }
            settings.MicrophoneIndex = ParseIndex(MicrophoneComboBox.SelectedItem.ToString());

            if (!int.TryParse(MinIntervalTextBox.Text, out int minInt) ||
                !int.TryParse(MaxIntervalTextBox.Text, out int maxInt) ||
                minInt < 0 || maxInt < minInt)
            {
                error = Strings.ErrInvalidInterval;
                return false;
            }
            settings.MinIntervalMinutes = minInt;
            settings.MaxIntervalMinutes = maxInt;

            if (!int.TryParse(MinDurationTextBox.Text, out int minDur) ||
                !int.TryParse(MaxDurationTextBox.Text, out int maxDur) ||
                minDur < 1 || maxDur < minDur)
            {
                error = Strings.ErrInvalidDuration;
                return false;
            }
            settings.MinDurationSeconds = minDur;
            settings.MaxDurationSeconds = maxDur;

            settings.TakeScreenshots = ScreenshotsCheckBox.IsChecked == true;

            return true;
        }

        private int ParseIndex(string? text)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            int colon = text.IndexOf(':');
            if (colon < 0) return -1;
            return int.TryParse(text.Substring(0, colon), out int idx) ? idx : -1;
        }

        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogTextBox.AppendText($"[{timestamp}] {message}\n");
            LogTextBox.ScrollToEnd();
        }
    }
}