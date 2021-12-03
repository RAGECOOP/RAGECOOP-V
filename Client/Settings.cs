namespace CoopClient
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Settings
    {
        internal string Username { get; set; } = "Player";
        internal string LastServerAddress { get; set; } = "127.0.0.1:4499";
        internal bool FlipMenu { get; set; } = false;
        internal int StreamedNpc { get; set; } = 10;
    }
}
