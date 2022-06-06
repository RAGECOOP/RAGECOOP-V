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
        public string MasterServer { get; set; } = "[AUTO]";
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

        /// <summary>
        /// Disable world NPC traffic, mission entities won't be affected
        /// </summary>
        public bool DisableTraffic { get; set; } = true;

        /// <summary>
        /// Bring up pause menu but don't freeze time when FrontEndPauseAlternate(Esc) is pressed.
        /// </summary>
        public bool DisableAlternatePause { get; set; } = true;

        /// <summary>
        /// The game won't spawn more NPC traffic if the limit is exceeded. -1 for unlimited (not recommended).
        /// </summary>
        public int WorldVehicleSoftLimit { get; set; } = 35;
    }
}
