namespace RandomRec
{
    /// <summary>
    /// Recording settings passed to RecorderService.
    /// </summary>
    class RecorderSettings
    {
        public string OutputFolder { get; set; } = "C:\\Records";

        // Индексы для OpenCV (превью, скриншоты)
        public int CameraIndex { get; set; }
        public int MicrophoneIndex { get; set; }

        // Имена для ffmpeg (запись)
        public string CameraName { get; set; } = "";
        public string MicrophoneName { get; set; } = "";

        public int MinIntervalMinutes { get; set; } = 30;
        public int MaxIntervalMinutes { get; set; } = 120;
        public int MinDurationSeconds { get; set; } = 30;
        public int MaxDurationSeconds { get; set; } = 120;
        public bool TakeScreenshots { get; set; } = true;

        // Игровой режим "найди запись": после записи файл прячется в случайное место.
        public bool GameMode { get; set; } = false;
    }
}