using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using GTA;

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

        private void OnConnection(bool connected, string reason = null)
        {
            if (connected)
            {
                CoopClient.Interface.SendChatMessage("Mod", "Welcome!");
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

            CoopClient.Interface.SendDataToAll("FirstScript", 0, new TestPacketClass() { A = 5, B = 15 }.SerializeToByteArray());
            CoopClient.Interface.SendDataToAll("FirstScript", 1, new SetPlayerTimePacket() { Hours = 0, Minutes = 0, Seconds = 0 }.SerializeToByteArray());
        }

        private void OnModPacketReceived(long from, string mod, byte customID, byte[] bytes)
        {
            if (mod != "FirstScript")
            {
                return;
            }

            switch (customID)
            {
                case 0:
                    TestPacketClass testPacketClass = bytes.Deserialize<TestPacketClass>();

                    GTA.UI.Notification.Show($"ModPacket(0)({from}): A[{testPacketClass.A}] B[{testPacketClass.B}]");
                    break;
                case 1:
                    GTA.UI.Notification.Show($"ModPacket(0)({from}): Nice!");
                    break;
                default:
                    GTA.UI.Notification.Show($"ModPacket({from}): ~r~Unknown customID!");
                    break;
            }
        }
    }

    public static class CustomSerializer
    {
        public static byte[] SerializeToByteArray(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T Deserialize<T>(this byte[] byteArray) where T : class
        {
            if (byteArray == null)
            {
                return null;
            }

            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryFormatter binForm = new BinaryFormatter();
                memStream.Write(byteArray, 0, byteArray.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                T obj = (T)binForm.Deserialize(memStream);
                return obj;
            }
        }
    }
}
