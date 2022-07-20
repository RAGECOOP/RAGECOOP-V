using System.Threading;
using System.Threading.Tasks;

namespace RageCoop.Client
{
    internal static class Statistics
    {
        public static int BytesDownPerSecond { get; private set; }
        public static int BytesUpPerSecond { get; private set; }
        static Statistics()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var bu = Networking.Client.Statistics.SentBytes;
                    var bd = Networking.Client.Statistics.ReceivedBytes;
                    Thread.Sleep(1000);
                    BytesUpPerSecond=Networking.Client.Statistics.SentBytes-bu;
                    BytesDownPerSecond=Networking.Client.Statistics.ReceivedBytes-bd;
                }
            });
        }
    }
}
