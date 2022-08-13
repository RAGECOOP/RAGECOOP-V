using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using NAudio.Wave;

namespace RageCoop.Client.Sync
{
    internal static class Voice
    {
        public static bool IsRecording = false;

        private static WaveInEvent _waveIn;
        private static BufferedWaveProvider _waveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

        private static Thread _thread;

        public static void StopRecording()
        {
            if (!IsRecording)
                return;

            _waveIn.StopRecording();
            GTA.UI.Notification.Show("STOPPED [1]");
        }

        public static void InitRecording()
        {
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

            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                BufferMilliseconds = 20,
                NumberOfBuffers = 1,
                WaveFormat = _waveProvider.WaveFormat
            };
            _waveIn.DataAvailable += WaveInDataAvailable;
            _waveIn.RecordingStopped += (object sender, StoppedEventArgs e) =>
            {
                IsRecording = false;
            };
            GTA.UI.Notification.Show("INIT");
        }

        public static void StartRecording()
        {
            if (IsRecording)
                return;

            IsRecording = true;
            _waveIn.StartRecording();
            GTA.UI.Notification.Show("STARTED");
        }

        private static void WaveInDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_waveIn == null)
                return;

            try
            {
                _waveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
                Networking.SendVoiceMessage(e.Buffer);
            } catch (Exception ex)
            {
                // if some happens along the way...
            }
        }
    }
}
