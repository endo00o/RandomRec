namespace RandomRec
{
    /// <summary>
    /// Recording settings passed to RecorderService.
    /// </summary>
    class RecorderSettings
    {
        public string OutputFolder { get; set; } = "C:\\Records";
        public int CameraIndex { get; set; }
        public int MicrophoneIndex { get; set; }
        public int MinIntervalMinutes { get; set; } = 30;
        public int MaxIntervalMinutes { get; set; } = 120;
        public int MinDurationSeconds { get; set; } = 30;
        public int MaxDurationSeconds { get; set; } = 120;
        public bool TakeScreenshots { get; set; } = true;
    }
}
