using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RandomRec
{
    /// <summary>
    /// "You lost" screen shown after the round timer runs out.
    /// Offers to play again or return to the main window.
    /// </summary>
    public partial class GameOverWindow : Window
    {
        public Action? PlayAgainRequested;
        public Action? BackToMainRequested;

        public GameOverWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsWithinButton(e.OriginalSource as DependencyObject)) return;
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private static bool IsWithinButton(DependencyObject? d)
        {
            while (d != null)
            {
                if (d is Button) return true;
                d = VisualTreeHelper.GetParent(d);
            }
            return false;
        }

        private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            PlayAgainRequested?.Invoke();
        }

        private void BackToMainButton_Click(object sender, RoutedEventArgs e)
        {
            BackToMainRequested?.Invoke();
        }
    }
}
