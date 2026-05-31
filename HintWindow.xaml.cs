using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace RandomRec
{
    /// <summary>
    /// Top-left overlay that shows a hint about where the recording is hidden.
    /// The text is updated over time by GameSession (vaguer -> more specific).
    /// </summary>
    public partial class HintWindow : Window
    {
        public HintWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the hint text. When <paramref name="animate"/> is true, plays a quick fade
        /// so a new hint visibly "refreshes". The very first hint passes false (the window's
        /// own appear-animation already fades it in).
        /// </summary>
        public void SetHint(string text, bool animate = true)
        {
            HintText.Text = text;

            if (animate)
            {
                var fade = new DoubleAnimation(0.4, 1.0, TimeSpan.FromSeconds(0.25));
                BeginAnimation(OpacityProperty, fade);
            }
        }
    }
}
