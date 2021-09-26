using System.Linq;

using CoopServer;

namespace FirstGameMode
{
    class Commands
    {
        [Command("hello")]
        public static void HelloCommand(CommandContext ctx)
        {
            ctx.Client.SendChatMessage("Hello " + ctx.Client.Player.Username + " :)");
        }

        [Command("inrange")]
        public static void InRangeCommand(CommandContext ctx)
        {
            if (ctx.Client.Player.IsInRangeOf(new LVector3(0f, 0f, 75f), 7f))
            {
                ctx.Client.SendChatMessage("You are in range! :)");
            }
            else
            {
                ctx.Client.SendChatMessage("You are not in range! :(");
            }
        }

        [Command("online")]
        public static void OnlineCommand(CommandContext ctx)
        {
            ctx.Client.SendChatMessage(API.GetAllClientsCount() + " player online!");
        }

        [Command("kick")]
        public static void KickCommand(CommandContext ctx)
        {
            if (ctx.Args.Length < 2)
            {
                ctx.Client.SendChatMessage("Please use \"/kick <USERNAME> <REASON>\"");
                return;
            }

            ctx.Client.Kick(ctx.Args.Skip(1).ToArray());
        }

        [Command("setweather")]
        public static void SetWeatherCommand(CommandContext ctx)
        {
            int hours, minutes, seconds;

            if (ctx.Args.Length < 3)
            {
                ctx.Client.SendChatMessage("Please use \"/setweather <HOURS> <MINUTES> <SECONDS>\"");
                return;
            }
            else if (!int.TryParse(ctx.Args[0], out hours) || !int.TryParse(ctx.Args[1], out minutes) || !int.TryParse(ctx.Args[2], out seconds))
            {
                ctx.Client.SendChatMessage("Please use \"/setweather <NUMBER> <NUMBER> <NUMBER>\"");
                return;
            }

            ctx.Client.SendNativeCall(0x47C3B5848C3E45D8, hours, minutes, seconds);
        }

        [Command("upp")]
        public static void UpdatePlayerPositionCommand(CommandContext ctx)
        {
            ctx.Client.SetData("ShowPlayerPosition", !ctx.Client.GetData<bool>("ShowPlayerPosition"));
        }
    }
}
