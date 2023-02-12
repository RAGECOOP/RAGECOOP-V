namespace RageCoop.Client.Scripting
{
    public static unsafe partial class APIBridge
    {

        public static class Config
        {
            public static System.String Username => GetConfig<System.String>("Username");
            public static System.Boolean EnableAutoRespawn => GetConfig<System.Boolean>("EnableAutoRespawn");
            public static GTA.BlipColor BlipColor => GetConfig<GTA.BlipColor>("BlipColor");
            public static GTA.BlipSprite BlipSprite => GetConfig<GTA.BlipSprite>("BlipSprite");
            public static System.Single BlipScale => GetConfig<System.Single>("BlipScale");
            public static System.Boolean ShowPlayerNameTag => GetConfig<System.Boolean>("ShowPlayerNameTag");

        }

        #region PROPERTIES

        public static System.Int32 LocalPlayerID => GetProperty<System.Int32>("LocalPlayerID");
        public static System.Boolean IsOnServer => GetProperty<System.Boolean>("IsOnServer");
        public static System.Net.IPEndPoint ServerEndPoint => GetProperty<System.Net.IPEndPoint>("ServerEndPoint");
        public static System.Boolean IsMenuVisible => GetProperty<System.Boolean>("IsMenuVisible");
        public static System.Boolean IsChatFocused => GetProperty<System.Boolean>("IsChatFocused");
        public static System.Boolean IsPlayerListVisible => GetProperty<System.Boolean>("IsPlayerListVisible");
        public static System.Version CurrentVersion => GetProperty<System.Version>("CurrentVersion");
        public static System.Collections.Generic.Dictionary<System.Int32, RageCoop.Client.Scripting.PlayerInfo> Players => GetProperty<System.Collections.Generic.Dictionary<System.Int32, RageCoop.Client.Scripting.PlayerInfo>>("Players");


        #endregion

        #region FUNCTIONS

        public static void Connect(System.String address) => InvokeCommand("Connect", address);
        public static void Disconnect() => InvokeCommand("Disconnect");
        public static System.Collections.Generic.List<RageCoop.Core.ServerInfo> ListServers() => InvokeCommand<System.Collections.Generic.List<RageCoop.Core.ServerInfo>>("ListServers");
        public static void LocalChatMessage(System.String from, System.String message) => InvokeCommand("LocalChatMessage", from, message);
        public static void SendChatMessage(System.String message) => InvokeCommand("SendChatMessage", message);
        public static RageCoop.Client.Scripting.ClientResource GetResource(System.String name) => InvokeCommand<RageCoop.Client.Scripting.ClientResource>("GetResource", name);
        public static System.Object GetConfig(System.String name) => InvokeCommand<System.Object>("GetConfig", name);
        public static void SetConfig(System.String name, System.String jsonVal) => InvokeCommand("SetConfig", name, jsonVal);


        #endregion
    }
}
