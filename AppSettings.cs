using System;
using System.IO;
using System.Text.Json;

namespace RandomRec
{
    /// <summary>
    /// Application settings, saved to settings.json next to the executable.
    /// </summary>
    public class AppSettings
    {
        public string Language { get; set; } = "";  // "" = auto-detect by system culture

        public string OutputFolder { get; set; } = "C:\\Records";
        public int MinIntervalMinutes { get; set; } = 30;
        public int MaxIntervalMinutes { get; set; } = 120;
        public int MinDurationSeconds { get; set; } = 30;
        public int MaxDurationSeconds { get; set; } = 120;
        public int CameraIndex { get; set; } = 0;
        public int MicrophoneIndex { get; set; } = 0;
        public bool TakeScreenshots { get; set; } = true;
        public bool SilentMode { get; set; } = false;

        private static string FilePath =>
            Path.Combine(AppContext.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch
            {
                // Broken file — ignore and return defaults.
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Non-critical if it fails.
            }
        }
    }
}
