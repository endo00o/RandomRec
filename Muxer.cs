using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RandomRec
{
    /// <summary>
    /// Merges a video file and an audio file into a single MP4 using ffmpeg.
    /// </summary>
    static class Muxer
    {
        /// <summary>
        /// Path to ffmpeg.exe. Defaults to next to our own executable.
        /// </summary>
        public static string FfmpegPath { get; set; } =
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");

        /// <summary>
        /// Merges video.avi and audio.wav into output.mp4.
        /// Throws on failure with the ffmpeg error output included.
        /// </summary>
        public static async Task MuxAsync(string videoPath, string audioPath, string outputPath)
        {
            if (!File.Exists(FfmpegPath))
                throw new FileNotFoundException($"ffmpeg.exe not found at: {FfmpegPath}");

            if (!File.Exists(videoPath))
                throw new FileNotFoundException($"Video file not found: {videoPath}");

            if (!File.Exists(audioPath))
                throw new FileNotFoundException($"Audio file not found: {audioPath}");

            // If the output file already exists, ffmpeg would prompt to overwrite.
            // Delete it beforehand so ffmpeg doesn't hang waiting for input.
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            // Arguments:
            //   -i video.avi -i audio.wav     : two inputs
            //   -c:v libx264 -preset veryfast : re-encode video as H.264
            //   -c:a aac -b:a 128k            : audio as AAC at 128 kbps
            //   -shortest                     : output length = shortest input
            //   output.mp4                    : result file
            string args =
                $"-i \"{videoPath}\" -i \"{audioPath}\" " +
                "-c:v libx264 -preset veryfast " +
                "-c:a aac -b:a 128k " +
                "-shortest " +
                $"\"{outputPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            // ffmpeg writes almost everything to stderr (not because of errors — that's just its convention).
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception(
                    $"ffmpeg exited with code {process.ExitCode}. Details:\n{stderr}");
            }
        }
    }
}
