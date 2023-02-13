using System.Threading;
using System.Threading.Tasks;

namespace RageCoop.Client
{
    internal static class Statistics
    {
        static Statistics()
        {
            ThreadManager.CreateThread(() =>
            {
                while (!IsUnloading)
                {
                    var bu = Networking.Peer.Statistics.SentBytes;
                    var bd = Networking.Peer.Statistics.ReceivedBytes;
                    Thread.Sleep(1000);
                    BytesUpPerSecond = Networking.Peer.Statistics.SentBytes - bu;
                    BytesDownPerSecond = Networking.Peer.Statistics.ReceivedBytes - bd;
                }
            },"Statistics");
        }

        public static int BytesDownPerSecond { get; private set; }
        public static int BytesUpPerSecond { get; private set; }
    }
}