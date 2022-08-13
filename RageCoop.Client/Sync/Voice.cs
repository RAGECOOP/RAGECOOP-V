using System.Threading;

using NAudio.Wave;

namespace RageCoop.Client.Sync
{
    internal static class Voice
    {
        private static bool _initialized = false;
        public static bool IsRecording = false;

        private static WaveInEvent _waveIn;
        private static BufferedWaveProvider _waveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

        private static Thread _thread;

        public static bool WasInitialized() => _initialized;
        public static void ClearAll()
        {
            _waveProvider.ClearBuffer();

            StopRecording();

            if (_thread != null && _thread.IsAlive)
            {
                _thread.Abort();
                _thread = null;
            }

            _initialized = false;
        }

        public static void StopRecording()
        {
            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;
            }

            IsRecording = false;
        }

        public static void InitRecording()
        {
            if (_initialized)
                return;

            // I tried without thread but the game will lag without
            _thread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    using (var wo = new WaveOutEvent())
                    {
                        wo.Init(_waveProvider);
                        wo.Play();

                        while (wo.PlaybackState == PlaybackState.Playing)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }));
            _thread.Start();

            _initialized = true;
        }

        public static void StartRecording()
        {
            if (IsRecording)
                return;

            IsRecording = true;

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
            if (_waveIn == null || !IsRecording)
                return;

            Networking.SendVoiceMessage(e.Buffer, e.BytesRecorded);
        }
    }
}
