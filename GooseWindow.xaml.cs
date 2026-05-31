using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;

namespace RandomRec
{
    /// <summary>
    /// Bottom-left "Toasty!"-style goose: slides up from the corner, honks,
    /// then slides back and closes itself. Shown when the round timer hits 0:00.
    /// </summary>
    public partial class GooseWindow : Window
    {
        private IWavePlayer? _wave;
        private Mp3FileReader? _reader;
        private Stream? _resStream;
        private DispatcherTimer? _closeTimer;

        public GooseWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                PlayHonk();
                // Закрываемся после того, как гусь уехал (анимация ~3 c).
                _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.2) };
                _closeTimer.Tick += (_, __) => { _closeTimer?.Stop(); Close(); };
                _closeTimer.Start();
            };
        }

        private void PlayHonk()
        {
            try
            {
                var asm = typeof(GooseWindow).Assembly;
                string? res = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("honk-sound.mp3", StringComparison.OrdinalIgnoreCase));
                if (res == null) return;

                _resStream = asm.GetManifestResourceStream(res);
                if (_resStream == null) return;

                _reader = new Mp3FileReader(_resStream);
                _wave = new WaveOutEvent();
                _wave.Init(_reader);
                _wave.Play();
            }
            catch
            {
                // Если что-то со звуком не так — гусь всё равно появится, просто молча.
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try { _wave?.Stop(); _wave?.Dispose(); } catch { }
            try { _reader?.Dispose(); } catch { }
            try { _resStream?.Dispose(); } catch { }
        }
    }
}
