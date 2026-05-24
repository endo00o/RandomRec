using System;
using NAudio.Wave;

namespace RandomRec
{
    /// <summary>
    /// Records audio from a microphone into a WAV file. One instance per recording.
    /// </summary>
    class AudioRecorder
    {
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private bool _isRecording;

        public bool IsRecording => _isRecording;

        public void Start(int deviceIndex, string outputPath)
        {
            if (_isRecording)
                throw new InvalidOperationException("Recording is already in progress");

            // Source setup: microphone, 44.1 kHz, mono, 16-bit.
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceIndex,
                WaveFormat = new WaveFormat(44100, 16, 1)
            };

            // Open file writer with the same format.
            _writer = new WaveFileWriter(outputPath, _waveIn.WaveFormat);

            // Whenever NAudio delivers a buffer, write it to the file.
            _waveIn.DataAvailable += OnDataAvailable;

            // When NAudio signals "stopped", release resources.
            _waveIn.RecordingStopped += OnRecordingStopped;

            _waveIn.StartRecording();
            _isRecording = true;
        }

        public void Stop()
        {
            if (!_isRecording) return;

            _waveIn?.StopRecording();
            // Cleanup happens in OnRecordingStopped.
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            _writer?.Dispose();
            _writer = null;

            _waveIn?.Dispose();
            _waveIn = null;

            _isRecording = false;
        }
    }
}
