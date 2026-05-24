<div align="center">
  <img src="assets/randomrec.png" width="100" alt="Random Recorder">
  <h1>Random Recorder</h1>
  <p><i>Captures short random snippets of camera and microphone for life-logging or creative recording.</i></p>
</div>

---

## What it does

Random Recorder runs quietly in the background. At random intervals it records a short clip from your webcam and microphone, then waits for the next one. Perfect for:

- **Life-logging** — capture authentic, unposed moments throughout the day
- **Creative projects** — generate raw material for collages, video diaries, or art
- **Self-observation** — see how you actually look and sound when you're not "on"

## Features

- 🎲 Recordings trigger at random times within a range you set (e.g. every 30–120 minutes)
- 🎬 Each recording lasts a random duration (e.g. 30–120 seconds)
- 📸 Optional screenshot capture alongside each video
- 🎥 Pick your camera and microphone from any connected device
- 🔴 Live preview with a REC indicator when recording is active
- 🌍 Available in English and Russian (auto-detected by system, switchable in UI)
- 💾 All settings persist between launches

## Screenshots

<!-- Add screenshots here after publishing -->

## Installation

1. Download the latest release from the [Releases page](https://github.com/endo00o/random-recorder/releases)
2. Download `ffmpeg.exe` from [gyan.dev](https://www.gyan.dev/ffmpeg/builds/) — the `release-essentials` build is enough
3. Place `ffmpeg.exe` next to `RandomRec.exe`
4. Run `RandomRec.exe`

> ⚠️ Requires Windows 10 or later. .NET 8 Runtime is bundled.

## Usage

1. Pick a folder where recordings will be saved
2. Set the interval range (in minutes) between recordings
3. Set the duration range (in seconds) for each recording
4. Choose your camera and microphone
5. Hit **Start** — the first recording begins immediately, then random ones follow
6. Hit **Stop** to end the session

Recordings are saved as `.mp4` files with timestamped names like `rec_2026-05-24_14-32-18.mp4`. Screenshots (if enabled) are saved alongside as `.png`.

## Building from source

Requires:
- Visual Studio 2022 with the **.NET desktop development** workload
- .NET 8 SDK

Clone, open `RandomRec.csproj` in Visual Studio, build and run.

## Tech stack

- C# / WPF (.NET 8)
- [OpenCvSharp](https://github.com/shimat/opencvsharp) — camera capture
- [NAudio](https://github.com/naudio/NAudio) — microphone capture
- [DirectShowLib](https://github.com/Sascha-L/WPF-MediaKit) — enumerating real device names
- [ffmpeg](https://ffmpeg.org/) — final muxing into MP4

## License

[MIT](LICENSE) © 2026 endo

## Author

Made by **endo** — find me on [GitHub](https://github.com/endo00o) or [Telegram](https://t.me/endo0_0).