using System;

using GTA;
using GTA.Native;

namespace FirstScript
{
    [Serializable]
    public class TestPacketClass
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    [Serializable]
    public class SetPlayerTimePacket
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
    }

    public class Main : Script
    {
        public Main()
        {
            Tick += OnTick;
            Interval += 3000;

            CoopClient.Interface.OnModPacketReceived += OnModPacketReceived;
            CoopClient.Interface.OnConnection += OnConnection;
        }

        private void OnConnection(bool connected, long id, string reason = null)
        {
            if (connected)
            {
                CoopClient.Interface.LocalChatMessage("Mod", "Welcome!");
            }
            else
            {
                GTA.UI.Notification.Show("~r~Mod~s~: ~b~C(°-°)D");
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!CoopClient.Interface.IsOnServer())
            {
                return;
            }

            CoopClient.Interface.SendDataToAll("FirstScript", 0, CoopClient.CoopSerializer.CSerialize(new TestPacketClass() { A = 5, B = 15 }));
            CoopClient.Interface.SendDataToPlayer(CoopClient.Interface.GetLocalID(), "FirstScript", 1, CoopClient.CoopSerializer.CSerialize(new SetPlayerTimePacket() { Hours = 1, Minutes = 2, Seconds = 3 }));
        }

        private void OnModPacketReceived(long from, string mod, byte customID, byte[] bytes)
        {
            if (mod != "FirstScript")
            {
                return;
            }

            CoopClient.Entities.EntitiesPlayer fromClient;

            switch (customID)
            {
                case 0:
                    TestPacketClass testPacketClass = CoopClient.CoopSerializer.CDeserialize<TestPacketClass>(bytes);

                    fromClient = CoopClient.Interface.GetPlayer(from);
                    if (fromClient == null)
                    {
                        if (from != CoopClient.Interface.GetLocalID())
                        {
                            GTA.UI.Notification.Show($"ClientID [{from}] not found!");
                        }
                        else
                        {
                            GTA.UI.Notification.Show($"ModPacket(0): A[{testPacketClass.A}] B[{testPacketClass.B}]");
                        }
                    }
                    else
                    {
                        GTA.UI.Notification.Show($"ModPacket(0)({fromClient.Username}): A[{testPacketClass.A}] B[{testPacketClass.B}]");
                    }
                    break;
                case 1:
                    SetPlayerTimePacket setPlayerTimePacket = CoopClient.CoopSerializer.CDeserialize<SetPlayerTimePacket>(bytes);

                    fromClient = CoopClient.Interface.GetPlayer(from);
                    if (fromClient != null)
                    {
                        GTA.UI.Notification.Show($"Player {fromClient.Username} changed the time to: {setPlayerTimePacket.Hours}, {setPlayerTimePacket.Minutes}, {setPlayerTimePacket.Seconds}");
                    }

                    Function.Call(Hash.SET_CLOCK_TIME, setPlayerTimePacket.Hours, setPlayerTimePacket.Minutes, setPlayerTimePacket.Seconds);
                    break;
                default:
                    GTA.UI.Notification.Show($"ModPacket({from}): ~r~Unknown customID!");
                    break;
            }
        }
    }
}
