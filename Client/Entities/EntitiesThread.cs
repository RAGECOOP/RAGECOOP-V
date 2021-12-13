using System;
using System.Collections.Generic;
using System.Linq;

using GTA;

namespace CoopClient.Entities
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class EntitiesThread : Script
    {
        /// <summary>
        /// Don't use it!
        /// </summary>
        public EntitiesThread()
        {
            Tick += OnTick;
            Interval = Util.GetGameMs<int>();
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

                ulong tickCount = Util.GetTickCount64();
                foreach (KeyValuePair<long, EntitiesNpc> npc in new Dictionary<long, EntitiesNpc>(localNpcs))
                {
                    if ((tickCount - npc.Value.LastUpdateReceived) > 3000)
                    {
                        if (npc.Value.Character != null && npc.Value.Character.Exists() && !npc.Value.Character.IsDead)
                        {
                            npc.Value.Character.Kill();
                            npc.Value.Character.MarkAsNoLongerNeeded();
                            npc.Value.Character.Delete();
                        }

                        if (npc.Value.MainVehicle != null && npc.Value.MainVehicle.Exists() && !npc.Value.MainVehicle.IsDead && npc.Value.MainVehicle.IsSeatFree(VehicleSeat.Driver) && npc.Value.MainVehicle.PassengerCount == 0)
                        {
                            npc.Value.MainVehicle.MarkAsNoLongerNeeded();
                            npc.Value.MainVehicle.Delete();
                        }

                        localNpcs.Remove(npc.Key);
                        Main.Npcs.Remove(npc.Key);
                    }
                }
            }

            foreach (EntitiesNpc npc in localNpcs.Values)
            {
                npc.DisplayLocally(null);
            }

            // Only if that player wants to share his NPCs with others
            if (Main.ShareNpcsWithPlayers)
            {
                // Send all npcs from the current player
                foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 150f)
                    .Where(p => p.Handle != Game.Player.Character.Handle && p.RelationshipGroup != Main.RelationshipGroup)
                    .OrderBy(p => (p.Position - Game.Player.Character.Position).Length()))
                {
                    Main.MainNetworking.SendNpcData(ped);
                }
            }
        }
    }
}
