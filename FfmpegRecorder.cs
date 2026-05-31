using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RandomRec
{
    /// <summary>
    /// Records video+audio directly via ffmpeg dshow capture.
    /// Outputs final MP4 without any muxing step.
    /// ffmpeg.exe is embedded into this assembly and extracted on first use.
    /// </summary>
    class FfmpegRecorder
    {
        private static string? _ffmpegPath;

        /// <summary>
        /// Path to ffmpeg.exe. On first access the embedded ffmpeg is extracted
        /// to %LOCALAPPDATA%\RandomRec\ffmpeg.exe and that path is returned.
        /// Can still be overridden manually by assigning a value.
        /// </summary>
        public static string FfmpegPath
        {
            get => _ffmpegPath ??= EnsureFfmpegExtracted();
            set => _ffmpegPath = value;
        }

        /// <summary>
        /// Extracts the embedded ffmpeg.exe into a per-user folder if it isn't there yet
        /// (or if the existing file's size differs, e.g. after a version bump).
        /// </summary>
        private static string EnsureFfmpegExtracted()
        {
            string targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RandomRec");
            Directory.CreateDirectory(targetDir);

            string targetPath = Path.Combine(targetDir, "ffmpeg.exe");

            var assembly = typeof(FfmpegRecorder).Assembly;

            // Find the embedded resource regardless of the exact namespace prefix.
            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Embedded ffmpeg.exe resource not found.");

            using var resourceStream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not open embedded resource: {resourceName}");

            // Re-extract only if the file is missing or its size doesn't match the resource.
            bool needExtract = !File.Exists(targetPath)
                               || new FileInfo(targetPath).Length != resourceStream.Length;

            if (needExtract)
            {
                using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                resourceStream.CopyTo(fileStream);
            }

            return targetPath;
        }

        private Process? _process;
        private string? _outputPath;

        public bool IsRecording { get; private set; }

        /// <summary>
        /// Starts a recording. ffmpeg runs until StopAsync() is called.
        /// </summary>
        /// <param name="cameraName">DirectShow video device name, e.g. "WebCamera"</param>
        /// <param name="microphoneName">DirectShow audio device name, e.g. "Микрофон (4- fifine Microphone)"</param>
        /// <param name="outputMp4Path">Final MP4 path</param>
        public void Start(string cameraName, string microphoneName, string outputMp4Path)
        {
            if (IsRecording)
                throw new InvalidOperationException("Recording is already in progress");

            if (!File.Exists(FfmpegPath))
                throw new FileNotFoundException($"ffmpeg.exe not found at: {FfmpegPath}");

            _outputPath = outputMp4Path;

            // Single ffmpeg command captures camera + mic and writes MP4 directly.
            // Capture parameters BEFORE -i request the device to give us a specific mode.
            string args =
                "-y " +
                "-f dshow " +
                "-rtbufsize 100M " +
                "-framerate 30 " +
                "-video_size 1280x720 " +
                "-vcodec mjpeg " +
                $"-i video=\"{cameraName}\":audio=\"{microphoneName}\" " +
                "-c:v libx264 -preset veryfast -pix_fmt yuv420p " +
                "-c:a aac -b:a 128k " +
                $"\"{outputMp4Path}\"";

            var psi = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,   // нужно чтобы послать 'q' для graceful stop
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            _process = new Process { StartInfo = psi };
            _process.Start();

            // Сливаем stderr/stdout в никуда, чтобы буфера не переполнились
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();

            IsRecording = true;
        }

        /// <summary>
        /// Gracefully stops ffmpeg by sending 'q' to its stdin.
        /// This ensures the MP4 trailer is written properly.
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRecording || _process == null) return;

            try
            {
                // 'q' is ffmpeg's way to gracefully exit during recording
                await _process.StandardInput.WriteAsync('q');
                await _process.StandardInput.FlushAsync();
            }
            catch { /* process might be dying already */ }

            // Wait up to 5 seconds for graceful shutdown
            bool exited = _process.WaitForExit(5000);

            if (!exited)
            {
                // Kill if it didn't respond to 'q' — result MP4 might be broken though
                try { _process.Kill(); } catch { }
                _process.WaitForExit();
            }

            _process.Dispose();
            _process = null;
            IsRecording = false;
        }
    }
}
