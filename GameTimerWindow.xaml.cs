using System;
using System.Windows;

namespace RandomRec
{
    /// <summary>
    /// Top-center game overlay: shows the countdown and acts as the drop target
    /// for the file the player finds. Pure UI — the path check happens in GameSession.
    /// </summary>
    public partial class GameTimerWindow : Window
    {
        /// <summary>Raised with the dropped file paths so the session can verify them.</summary>
        public Action<string[]>? FilesDropped;

        public GameTimerWindow()
        {
            InitializeComponent();
        }

        public void SetTime(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            TimerText.Text = $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
        }

        public void ShowWrong()
        {
            StatusText.Text = GameText.WrongRecording;
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                SetHighlight(true);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            SetHighlight(false);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            SetHighlight(false);

            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            FilesDropped?.Invoke(files);
        }

        private void SetHighlight(bool on)
        {
            RootBorder.BorderThickness = new Thickness(on ? 2.5 : 1.5);
        }
    }
}