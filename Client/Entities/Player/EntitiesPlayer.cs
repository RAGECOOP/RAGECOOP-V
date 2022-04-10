using System;
using System.Drawing;
using System.Collections.Generic;

using GTA;
using GTA.Native;
using GTA.Math;

using LemonUI.Elements;

namespace CoopClient.Entities.Player
{
    /// <summary>
    /// ?
    /// </summary>
    public partial class EntitiesPlayer
    {
        /// <summary>
        /// ?
        /// </summary>
        public string Username { get; set; } = "Player";

        private bool _allDataAvailable = false;
        internal bool LastSyncWasFull { get; set; } = false;
        /// <summary>
        /// Get the last update = TickCount64()
        /// </summary>
        public ulong LastUpdateReceived { get; set; }
        /// <summary>
        /// Get the player latency
        /// </summary>
        public float Latency { get; internal set; }

        /// <summary>
        /// ?
        /// </summary>
        public Ped Character { get; internal set; }
        /// <summary>
        /// The latest character health (may not have been applied yet)
        /// </summary>
        public int Health { get; internal set; }
        private int _lastModelHash = 0;
        private int _currentModelHash = 0;
        /// <summary>
        /// The latest character model hash (may not have been applied yet)
        /// </summary>
        public int ModelHash
        {
            get => _currentModelHash;
            internal set
            {
                _lastModelHash = _lastModelHash == 0 ? value : _currentModelHash;
                _currentModelHash = value;
            }
        }
        private Dictionary<byte, short> _lastClothes = null;
        internal Dictionary<byte, short> Clothes { get; set; }
        /// <summary>
        /// The latest character position (may not have been applied yet)
        /// </summary>
        public Vector3 Position { get; internal set; }
        internal Vector3 Velocity { get; set; }
        internal Blip PedBlip = null;
        internal Vector3 AimCoords { get; set; }

        internal void Update()
        {
            // Check beforehand whether ped has all the required data
            if (!_allDataAvailable)
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
                            PedBlip.Name = Username;
                        }
                    }

                    return;
                }

                _allDataAvailable = true;
            }

            #region NOT_IN_RANGE
            if (!Game.Player.Character.IsInRange(Position, 500f))
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

                if (PedBlip != null && PedBlip.Exists())
                {
                    PedBlip.Position = Position;
                }
                else
                {
                    PedBlip = World.CreateBlip(Position);
                    PedBlip.Color = BlipColor.White;
                    PedBlip.Scale = 0.8f;
                    PedBlip.Name = Username;
                }

                return;
            }
            #endregion

            #region IS_IN_RANGE
            bool characterExist = Character != null && Character.Exists();

            if (!characterExist)
            {
                if (!CreateCharacter())
                {
                    return;
                }
            }
            else if (LastSyncWasFull)
            {
                if (ModelHash != _lastModelHash)
                {
                    Character.Kill();
                    Character.Delete();

                    if (!CreateCharacter())
                    {
                        return;
                    }
                }
                else if (!Clothes.Compare(_lastClothes))
                {
                    SetClothes();
                }
            }

            RenderNameTag();

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

        private void RenderNameTag()
        {
            if (Character.IsVisible && Character.IsInRange(Game.Player.Character.Position, 20f))
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

                new ScaledText(new PointF(0, 0), Username, 0.4f * sizeOffset, GTA.UI.Font.ChaletLondon)
                {
                    Outline = true,
                    Alignment = GTA.UI.Alignment.Center
                }.Draw();

                Function.Call(Hash.CLEAR_DRAW_ORIGIN);
            }
        }

        private bool CreateCharacter()
        {
            if (PedBlip != null && PedBlip.Exists())
            {
                PedBlip.Delete();
                PedBlip = null;
            }

            Model characterModel = ModelHash.ModelRequest();

            if (characterModel == null)
            {
                //GTA.UI.Notification.Show($"~r~(Character)Model ({CurrentModelHash}) cannot be loaded!");
                return false;
            }

            Character = World.CreatePed(characterModel, Position, Rotation.Z);
            characterModel.MarkAsNoLongerNeeded();

            Character.RelationshipGroup = Main.RelationshipGroup;
            Character.Health = Health;
            if (IsInVehicle)
            {
                Character.IsVisible = false;
            }
            Character.BlockPermanentEvents = true;
            Character.CanRagdoll = false;
            Character.IsInvincible = true;
            
            Character.CanSufferCriticalHits = false;

            // Add a new blip for the ped
            Character.AddBlip();
            Character.AttachedBlip.Color = BlipColor.White;
            Character.AttachedBlip.Scale = 0.8f;
            Character.AttachedBlip.Name = Username;

            Function.Call(Hash.SET_PED_CAN_EVASIVE_DIVE, Character.Handle, false);
            Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD, Character.Handle, false);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, Character.Handle, true);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED_BY_PLAYER, Character.Handle, Game.Player, true);
            Function.Call(Hash.SET_PED_GET_OUT_UPSIDE_DOWN_VEHICLE, Character.Handle, false);
            Function.Call(Hash.SET_PED_AS_ENEMY, Character.Handle, false);
            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, Character.Handle, true, true);

            SetClothes();

            return true;
        }

        private void SetClothes()
        {
            foreach (KeyValuePair<byte, short> cloth in Clothes)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character.Handle, cloth.Key, cloth.Value, 0, 0);
            }

            _lastClothes = Clothes;
        }
    }
}
