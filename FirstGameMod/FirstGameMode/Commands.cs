using CoopServer;
using System.Collections.Generic;

namespace FirstGameMode
{
    class Commands
    {
        [Command("set")]
        public static void TimeCommand(CommandContext ctx)
        {
            if (ctx.Args.Length < 1)
            {
                ctx.Client.SendChatMessage("Please use \"/set <OPTION> ...\"");
                return;
            }
            else if (ctx.Args.Length < 2)
            {
                ctx.Client.SendChatMessage($"Please use \"/set {ctx.Args[0]} ...\"");
                return;
            }

            switch (ctx.Args[0])
            {
                case "time":
                    int hours, minutes, seconds;

                    if (ctx.Args.Length < 4)
                    {
                        ctx.Client.SendChatMessage("Please use \"/set time <HOURS> <MINUTES> <SECONDS>\"");
                        return;
                    }
                    else if (!int.TryParse(ctx.Args[1], out hours) || !int.TryParse(ctx.Args[2], out minutes) || !int.TryParse(ctx.Args[3], out seconds))
                    {
                        ctx.Client.SendChatMessage($"Please use \"/set time <NUMBER> <NUMBER> <NUMBER>\"");
                        return;
                    }

                    ctx.Client.SendNativeCall(0x47C3B5848C3E45D8, hours, minutes, seconds);
                    break;
            }
        }

        [Command("inrange")]
        public static void InRangeCommand(CommandContext ctx)
        {
            List<string> list = new();
            API.GetAllClients().FindAll(x => x.Player.IsInRangeOf(ctx.Client.Player.Position, 15f)).ForEach(x => list.Add(x.Player.Username));

            ctx.Client.SendChatMessage(string.Join(", ", list.ToArray()));
        }
    }
}
