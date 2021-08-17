using System;
using System.Collections.Generic;
using System.Linq;

using GTA;

namespace CoopClient.Entities
{
    public class EntitiesThread : Script
    {
        public EntitiesThread()
        {
            Tick += OnTick;
            Interval = 1000 / 60;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading || !Main.MainNetworking.IsOnServer() || !Main.NpcsAllowed)
            {
                return;
            }

            Dictionary<long, EntitiesNpc> localNpcs = null;
            lock (Main.Npcs)
            {
                localNpcs = new Dictionary<long, EntitiesNpc>(Main.Npcs);
            }

            int tickCount = Environment.TickCount;
            for (int i = localNpcs.Count - 1; i >= 0; i--)
            {
                long key = localNpcs.ElementAt(i).Key;

                if ((tickCount - localNpcs[key].LastUpdateReceived) > 3500)
                {
                    if (localNpcs[key].Character != null && localNpcs[key].Character.Exists() && localNpcs[key].Health > 0)
                    {
                        localNpcs[key].Character.Kill();
                        localNpcs[key].Character.Delete();
                    }

                    if (localNpcs[key].MainVehicle != null && localNpcs[key].MainVehicle.Exists() && localNpcs[key].MainVehicle.PassengerCount == 0)
                    {
                        localNpcs[key].MainVehicle.Delete();
                    }

                    localNpcs.Remove(key);
                }
            }

            lock (Main.Npcs)
            {
                foreach (KeyValuePair<long, EntitiesNpc> npc in new Dictionary<long, EntitiesNpc>(Main.Npcs))
                {
                    if (!localNpcs.ContainsKey(npc.Key))
                    {
                        Main.Npcs.Remove(npc.Key);
                    }
                }
            }

            for (int i = 0; i < localNpcs.Count; i++)
            {
                localNpcs.ElementAt(i).Value.DisplayLocally(null);
            }

            // Only if that player wants to share his NPCs with others
            if (Main.ShareNpcsWithPlayers)
            {
                // Send all npcs from the current player
                foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 150f)
                    .Where(p => p.Handle != Game.Player.Character.Handle && !p.IsDead && p.RelationshipGroup != Main.RelationshipGroup)
                    .OrderBy(p => (p.Position - Game.Player.Character.Position).Length())
                    .Take((Main.MainSettings.StreamedNpc > 20 || Main.MainSettings.StreamedNpc < 0) ? 0 : Main.MainSettings.StreamedNpc))
                {
                    Main.MainNetworking.SendNpcData(ped);
                }
            }
        }
    }
}
