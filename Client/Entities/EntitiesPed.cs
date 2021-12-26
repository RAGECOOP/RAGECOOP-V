using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

using GTA;
using GTA.Native;
using GTA.Math;

using LemonUI.Elements;

namespace CoopClient.Entities
{
    /// <summary>
    /// ?
    /// </summary>
    public partial class EntitiesPed
    {
        // If this NPC is in a vehicle, we can find the handle of this vehicle in Main.NPCsVehicles[NPCVehHandle] and prevent multiple vehicles from being created
        internal long NPCVehHandle { get; set; } = 0;
        /// <summary>
        /// 0 = Nothing
        /// 1 = Character
        /// 2 = Vehicle
        /// </summary>
        private byte ModelNotFound = 0;
        private bool AllDataAvailable = false;
        internal bool LastSyncWasFull { get; set; } = false;
        /// <summary>
        /// PLEASE USE LastUpdateReceived
        /// </summary>
        private ulong LastUpdate;
        /// <summary>
        /// Get the last update = TickCount64()
        /// </summary>
        public ulong LastUpdateReceived
        {
            get => LastUpdate;
            internal set
            {
                if (LastUpdate != 0)
                {
                    LatencyAverager.Enqueue(value - LastUpdate);
                    if (LatencyAverager.Count >= 10)
                    {
                        LatencyAverager.Dequeue();
                    }
                }

                LastUpdate = value;
            }
        }
        private Queue<double> LatencyAverager = new Queue<double>();
        private double AverageLatency
        {
            get => LatencyAverager.Count == 0 ? 0 : LatencyAverager.Average();
        }
        /// <summary>
        /// Get the player latency
        /// </summary>
        public float Latency { get; internal set; } = 1.5f;

        /// <summary>
        /// ?
        /// </summary>
        public Ped Character { get; internal set; }
        /// <summary>
        /// The latest character health (may not have been applied yet)
        /// </summary>
        public int Health { get; internal set; }
        private int LastModelHash = 0;
        private int CurrentModelHash = 0;
        /// <summary>
        /// The latest character model hash (may not have been applied yet)
        /// </summary>
        public int ModelHash
        {
            get => CurrentModelHash;
            internal set
            {
                LastModelHash = LastModelHash == 0 ? value : CurrentModelHash;
                CurrentModelHash = value;
            }
        }
        private Dictionary<byte, short> LastClothes = null;
        internal Dictionary<byte, short> Clothes { get; set; }
        /// <summary>
        /// The latest character position (may not have been applied yet)
        /// </summary>
        public Vector3 Position { get; internal set; }
        internal Blip PedBlip = null;
        internal Vector3 AimCoords { get; set; }

        internal void DisplayLocally(string username)
        {
            /*
             * username: string
             *   string: null
             *     ped: npc
             *   string: value
             *     ped: player
             */

            // Check beforehand whether ped has all the required data
            if (!AllDataAvailable)
            {
                if (!LastSyncWasFull)
                {
                    if (Position != default)
                    {
                        if (PedBlip != null && PedBlip.Exists())
                        {
                            PedBlip.Position = Position;
                        }
                        else
                        {
                            PedBlip = World.CreateBlip(Position);
                            PedBlip.Color = BlipColor.White;
                            PedBlip.Scale = 0.8f;
                            PedBlip.Name = username;
                        }
                    }

                    return;
                }

                AllDataAvailable = true;
            }

            if (ModelNotFound != 0)
            {
                if (ModelNotFound == 1)
                {
                    if (CurrentModelHash != LastModelHash)
                    {
                        ModelNotFound = 0;
                    }
                }
                else
                {
                    if (CurrentVehicleModelHash != LastVehicleModelHash)
                    {
                        ModelNotFound = 0;
                    }
                }
            }

            #region NOT_IN_RANGE
            if (ModelNotFound != 0 || !Game.Player.Character.IsInRange(Position, 500f))
            {
                if (Character != null && Character.Exists())
                {
                    Character.Kill();
                    Character.MarkAsNoLongerNeeded();
                    Character.Delete();
                    Character = null;
                }

                if (MainVehicle != null && MainVehicle.Exists() && MainVehicle.IsSeatFree(VehicleSeat.Driver) && MainVehicle.PassengerCount == 0)
                {
                    MainVehicle.MarkAsNoLongerNeeded();
                    MainVehicle.Delete();
                    MainVehicle = null;
                }

                if (username != null)
                {
                    if (PedBlip != null && PedBlip.Exists())
                    {
                        PedBlip.Position = Position;
                    }
                    else
                    {
                        PedBlip = World.CreateBlip(Position);
                        PedBlip.Color = BlipColor.White;
                        PedBlip.Scale = 0.8f;
                        PedBlip.Name = username;
                    }
                }

                return;
            }
            #endregion

            #region IS_IN_RANGE
            bool characterExist = Character != null && Character.Exists();

            if (!characterExist)
            {
                if (!CreateCharacter(username))
                {
                    return;
                }
            }
            else if (LastSyncWasFull)
            {
                if (CurrentModelHash != LastModelHash)
                {
                    Character.Kill();
                    Character.Delete();

                    if (!CreateCharacter(username))
                    {
                        return;
                    }
                }
                else if (!Clothes.Compare(LastClothes))
                {
                    foreach (KeyValuePair<byte, short> cloth in Clothes)
                    {
                        Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character.Handle, cloth.Key, cloth.Value, 0, 0);
                    }

                    LastClothes = Clothes;
                }
            }

            if (username != null && Character.IsVisible && Character.IsInRange(Game.Player.Character.Position, 20f))
            {
                float sizeOffset;
                if (GameplayCamera.IsFirstPersonAimCamActive)
                {
                    Vector3 targetPos = Character.Bones[Bone.IKHead].Position + new Vector3(0, 0, 0.10f) + (Character.Velocity / Game.FPS);

                    Function.Call(Hash.SET_DRAW_ORIGIN, targetPos.X, targetPos.Y, targetPos.Z, 0);

                    sizeOffset = Math.Max(1f - ((GameplayCamera.Position - Character.Position).Length() / 30f), 0.30f);
                }
                else
                {
                    Vector3 targetPos = Character.Bones[Bone.IKHead].Position + new Vector3(0, 0, 0.35f) + (Character.Velocity / Game.FPS);

                    Function.Call(Hash.SET_DRAW_ORIGIN, targetPos.X, targetPos.Y, targetPos.Z, 0);

                    sizeOffset = Math.Max(1f - ((GameplayCamera.Position - Character.Position).Length() / 25f), 0.25f);
                }

                new ScaledText(new PointF(0, 0), username, 0.4f * sizeOffset, GTA.UI.Font.ChaletLondon)
                {
                    Outline = true,
                    Alignment = GTA.UI.Alignment.Center
                }.Draw();

                Function.Call(Hash.CLEAR_DRAW_ORIGIN);
            }

            if (Character.IsDead)
            {
                if (Health <= 0)
                {
                    return;
                }

                Character.IsInvincible = true;
                Character.Resurrect();
            }
            else if (Character.Health != Health)
            {
                Character.Health = Health;

                if (Health <= 0 && !Character.IsDead)
                {
                    Character.IsInvincible = false;
                    Character.Kill();
                    return;
                }
            }

            if (IsInVehicle)
            {
                DisplayInVehicle();
            }
            else
            {
                DisplayOnFoot();
            }
            #endregion
        }

        private bool CreateCharacter(string username)
        {
            if (PedBlip != null && PedBlip.Exists())
            {
                PedBlip.Delete();
                PedBlip = null;
            }

            Model characterModel = CurrentModelHash.ModelRequest();

            if (characterModel == null)
            {
                //GTA.UI.Notification.Show($"~r~(Character)Model ({CurrentModelHash}) cannot be loaded!");
                ModelNotFound = 1;
                return false;
            }

            Character = World.CreatePed(characterModel, Position, Rotation.Z);
            characterModel.MarkAsNoLongerNeeded();
            
            // ?
            Character.RelationshipGroup = Main.RelationshipGroup;

            if (IsInVehicle)
            {
                Character.IsVisible = false;
            }
            Character.BlockPermanentEvents = true;
            Character.CanRagdoll = false;
            Character.IsInvincible = true;
            Character.Health = Health;

            Character.CanBeTargetted = true;
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED_BY_PLAYER, Character.Handle, Game.Player, true);

            if (username != null)
            {
                // Add a new blip for the ped
                Character.AddBlip();
                Character.AttachedBlip.Color = BlipColor.White;
                Character.AttachedBlip.Scale = 0.8f;
                Character.AttachedBlip.Name = username;

                Function.Call(Hash.SET_PED_CAN_EVASIVE_DIVE, Character.Handle, false);
                Function.Call(Hash.SET_PED_GET_OUT_UPSIDE_DOWN_VEHICLE, Character.Handle, false);
            }

            foreach (KeyValuePair<byte, short> cloth in Clothes)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character.Handle, cloth.Key, cloth.Value, 0, 0);
            }

            return true;
        }
    }
}
