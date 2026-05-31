using System;
using System.Windows;

namespace RandomRec
{
    /// <summary>
    /// Bottom-right overlay with a Stop button. Lets the player abort the round.
    /// </summary>
    public partial class GameStopWindow : Window
    {
        public Action? StopRequested;

        public GameStopWindow()
        {
            InitializeComponent();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopRequested?.Invoke();
        }
    }
}
