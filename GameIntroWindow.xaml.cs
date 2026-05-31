using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RandomRec
{
    /// <summary>
    /// Game intro/splash screen shown before recording starts.
    /// Raises StartRequested when the player presses "Начать".
    /// </summary>
    public partial class GameIntroWindow : Window
    {
        /// <summary>Raised when the player chooses to start the game.</summary>
        public Action? StartRequested;

        /// <summary>Время раунда (секунды), выбранное игроком. Действительно после StartRequested.</summary>
        public int SelectedRoundSeconds { get; private set; } = 120;

        public GameIntroWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Не перетаскиваем окно, если клик пришёлся по кнопке.
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

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedRoundSeconds = ParseRoundSeconds();
            StartRequested?.Invoke();
        }

        // Читает и нормализует время из поля ввода (5..3600 секунд).
        private int ParseRoundSeconds()
        {
            if (int.TryParse(RoundTimeBox.Text.Trim(), out int s))
            {
                if (s < 5) s = 5;
                if (s > 3600) s = 3600;
                return s;
            }
            return 120;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}