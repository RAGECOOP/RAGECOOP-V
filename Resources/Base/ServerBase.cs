using RageCoop.Server.Scripting;
namespace RageCoop.Resources.Base
{
    public class ServerBase :ServerScript
    {
        public ServerBase()
        {
            API.RegisterCommand("kick", (ctx) =>
            {
                if (ctx.Args.Length<1) { return; }
                var reason = "eat poop";
                if(ctx.Args.Length>=2) { reason=ctx.Args[1]; }
                API.GetClientByUsername(ctx.Args[0]).Kick(reason);
            });
        }
    }
}