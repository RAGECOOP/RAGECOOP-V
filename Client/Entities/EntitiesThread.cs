using System;
using System.Collections.Generic;
using System.Linq;

using GTA;

namespace CoopClient.Entities
{
    public class EntitiesThread : Script
    {
        const int npcThreshold = 2500; // 2.5 seconds timeout

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

            lock (Main.Npcs)
            {
                // Remove all NPCs with a last update older than npcThreshold or display this npc
                foreach (KeyValuePair<string, EntitiesNpc> npc in new Dictionary<string, EntitiesNpc>(Main.Npcs))
                {
                    int tickCount = Environment.TickCount - npc.Value.LastUpdateReceived;

                    if (tickCount > 3500) // If the last update is older than 3.5s, then delete this npc completely
                    {
                        if (npc.Value.Character != null && npc.Value.Character.Exists() && npc.Value.Health > 0)
                        {
                            npc.Value.Character.Kill();
                            npc.Value.Character.Delete();
                        }

                        if (npc.Value.MainVehicle != null && npc.Value.MainVehicle.Exists() && npc.Value.MainVehicle.PassengerCount == 0)
                        {
                            npc.Value.MainVehicle.Delete();
                        }

                        Main.Npcs.Remove(npc.Key);
                    }
                    else if (tickCount > 1500) // If the last update is older than 1.5s, then delete this npc temporarily
                    {
                        if (npc.Value.Character != null && npc.Value.Character.Exists() && npc.Value.Health > 0)
                        {
                            npc.Value.Character.Kill();
                            npc.Value.Character.Delete();
                        }

                        if (npc.Value.MainVehicle != null && npc.Value.MainVehicle.Exists() && npc.Value.MainVehicle.PassengerCount == 0)
                        {
                            npc.Value.MainVehicle.Delete();
                        }
                    }
                    else
                    {
                        npc.Value.DisplayLocally(null);
                    }
                }
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
