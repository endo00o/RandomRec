using System.Globalization;
using System.Threading;
using System.Windows;

namespace RandomRec
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppSettings Settings { get; private set; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            Settings = AppSettings.Load();
            ApplyLanguage(Settings.Language);
            base.OnStartup(e);
        }

        public static void ApplyLanguage(string lang)
        {
            CultureInfo culture;
            if (string.IsNullOrEmpty(lang))
            {
                return;
            }
            else
            {
                try { culture = new CultureInfo(lang); }
                catch { return; }
            }

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}