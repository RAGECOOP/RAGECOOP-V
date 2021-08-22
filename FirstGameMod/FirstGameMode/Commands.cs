using System.Linq;

using CoopServer;

namespace FirstGameMode
{
    class Commands
    {
        [Command("hello")]
        public static void HelloCommand(CommandContext ctx)
        {
            API.SendChatMessageToPlayer(ctx.Player.Username, "Hello " + ctx.Player.Username + " :)");
        }

        [Command("inrange")]
        public static void InRangeCommand(CommandContext ctx)
        {
            if (ctx.Player.IsInRangeOf(new LVector3(0f, 0f, 75f), 7f))
            {
                API.SendChatMessageToPlayer(ctx.Player.Username, "You are in range! :)");
            }
            else
            {
                API.SendChatMessageToPlayer(ctx.Player.Username, "You are not in range! :(");
            }
        }

        [Command("online")]
        public static void OnlineCommand(CommandContext ctx)
        {
            API.SendChatMessageToPlayer(ctx.Player.Username, API.GetAllPlayersCount() + " player online!");
        }

        [Command("kick")]
        public static void KickCommand(CommandContext ctx)
        {
            if (ctx.Args.Length < 2)
            {
                API.SendChatMessageToPlayer(ctx.Player.Username, "Please use \"/kick <USERNAME> <REASON>\"");
                return;
            }

            API.KickPlayerByUsername(ctx.Args[0], ctx.Args.Skip(1).ToArray());
        }

        [Command("setweather")]
        public static void SetWeatherCommand(CommandContext ctx)
        {
            int hours, minutes, seconds;

            if (ctx.Args.Length < 3)
            {
                API.SendChatMessageToPlayer(ctx.Player.Username, "Please use \"/setweather <HOURS> <MINUTES> <SECONDS>\"");
                return;
            }
            else if (!int.TryParse(ctx.Args[0], out hours) || !int.TryParse(ctx.Args[1], out minutes) || !int.TryParse(ctx.Args[2], out seconds))
            {
                API.SendChatMessageToPlayer(ctx.Player.Username, "Please use \"/setweather <NUMBER> <NUMBER> <NUMBER>\"");
                return;
            }

            API.SendNativeCallToPlayer(ctx.Player.Username, 0x47C3B5848C3E45D8, hours, minutes, seconds);
        }

        [Command("upp")]
        public static void UpdatePlayerPositionCommand(CommandContext ctx)
        {
            Main.ShowPlayerPosition = !Main.ShowPlayerPosition;
        }
    }
}
