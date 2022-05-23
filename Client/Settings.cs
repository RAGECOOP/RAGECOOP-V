#undef DEBUG
using System.Windows.Forms;
namespace RageCoop.Client
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Don't use it!
        /// </summary>
        public string Username { get; set; } = "Player";
        /// <summary>
        /// Don't use it!
        /// </summary>
        public string LastServerAddress { get; set; } = "127.0.0.1:4499";
        /// <summary>
        /// Don't use it!
        /// </summary>
        public string MasterServer { get; set; } = "https://ragecoop.online/gtav/servers";
        /// <summary>
        /// Don't use it!
        /// </summary>
        public bool FlipMenu { get; set; } = false;

        /// <summary>
        /// LogLevel for RageCoop.
        /// 0:Trace, 1:Debug, 2:Info, 3:Warning, 4:Error
        /// </summary>
        public int LogLevel = 2;

        /// <summary>
        /// The key to open menu
        /// </summary>
        public Keys MenuKey { get; set; } = Keys.F9;

        /// <summary>
        /// The key to enter a vehicle as passenger.
        /// </summary>
        public Keys PassengerKey { get; set; }=Keys.G;

    }
}
