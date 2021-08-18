using System.Linq;

using CoopServer;

namespace FirstGameMode
{
    class Commands
    {
        [Command("hello")]
        public static void HelloCommand(CommandContext ctx)
        {
            ServerScript.SendChatMessageToPlayer(ctx.Player.Username, "Hello " + ctx.Player.Username + " :)");
        }

        [Command("inrange")]
        public static void InRangeCommand(CommandContext ctx)
        {
            if (ctx.Player.Ped.IsInRangeOf(new LVector3(0f, 0f, 75f), 7f))
            {
                ServerScript.SendChatMessageToPlayer(ctx.Player.Username, "You are in range! :)");
            }
            else
            {
                ServerScript.SendChatMessageToPlayer(ctx.Player.Username, "You are not in range! :(");
            }
        }

        [Command("online")]
        public static void OnlineCommand(CommandContext ctx)
        {
            ServerScript.SendChatMessageToPlayer(ctx.Player.Username, ServerScript.GetAllPlayersCount() + " player online!");
        }

        [Command("kick")]
        public static void KickCommand(CommandContext ctx)
        {
            if (ctx.Args.Length < 2)
            {
                ServerScript.SendChatMessageToPlayer(ctx.Player.Username, "Please use \"/kick <USERNAME> <REASON>\"");
                return;
            }

            ServerScript.KickPlayerByUsername(ctx.Args[0], ctx.Args.Skip(1).ToArray());
        }
    }
}
