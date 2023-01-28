using System.Threading;
using NAudio.Wave;

namespace RageCoop.Client
{
    internal static class Voice
    {
        private static bool _stopping = false;
        private static WaveInEvent _waveIn;

        private static readonly BufferedWaveProvider _waveProvider =
            new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

        private static Thread _thread;

        public static bool WasInitialized()
        {
            return _thread != null;
        }

        public static bool IsRecording()
        {
            return _waveIn != null;
        }

        public static void ClearAll()
        {
            _waveProvider.ClearBuffer();

            StopRecording();

            if (_thread != null && _thread.IsAlive)
            {
                _stopping = true;
                _thread.Join();
                _thread = null;
            }
        }

        public static void StopRecording()
        {
            if (!IsRecording())
                return;

            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }

        public static void Init()
        {
            if (WasInitialized())
                return;

            // I tried without thread but the game will lag without
            _thread = ThreadManager.CreateThread(() =>
            {
                while (!_stopping && !Main.IsUnloading)
                    using (var wo = new WaveOutEvent())
                    {
                        wo.Init(_waveProvider);
                        wo.Play();

                        while (wo.PlaybackState == PlaybackState.Playing) Thread.Sleep(100);
                    }
            },"Voice");
        }

        public static void StartRecording()
        {
            if (IsRecording())
                return;

            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                BufferMilliseconds = 20,
                NumberOfBuffers = 1,
                WaveFormat = _waveProvider.WaveFormat
            };
            _waveIn.DataAvailable += WaveInDataAvailable;

            _waveIn.StartRecording();
        }

        public static void AddVoiceData(byte[] buffer, int recorded)
        {
            _waveProvider.AddSamples(buffer, 0, recorded);
        }

        private static void WaveInDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!IsRecording())
                return;

            Networking.SendVoiceMessage(e.Buffer, e.BytesRecorded);
        }
    }
}