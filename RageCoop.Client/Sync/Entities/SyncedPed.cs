using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using RageCoop.Core;
using GTA;
using GTA.Native;
using GTA.Math;
using LemonUI.Elements;
using System.Security.Cryptography;

namespace RageCoop.Client
{
    /// <summary>
    /// ?
    /// </summary>
    internal class SyncedPed:SyncedEntity
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Create a local entity (outgoing sync)
        /// </summary>
        /// <param name="p"></param>
        public SyncedPed(Ped p)
        {
            ID=EntityPool.RequestNewID();
            p.CanWrithe=false;
            p.IsOnlyDamagedByPlayer=false;
            MainPed=p;
            OwnerID=Main.LocalPlayerID;

            Function.Call(Hash._SET_PED_CAN_PLAY_INJURED_ANIMS, false);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableHurt, true);
                        MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableMelee, true);

        }

        /// <summary>
        /// Create an empty character with ID
        /// </summary>
        public SyncedPed(int id)
        {
            ID=id;
            LastSynced=Main.Ticked;
        }
        #endregion
        #region PLAYER -- ONLY
        public string Username = "N/A";
        public Blip PedBlip = null;
        #endregion

        /// <summary>
        /// Indicates whether this ped is a player
        /// </summary>
        public bool IsPlayer { get { return (OwnerID==ID)&&(ID!=0); } }
        /// <summary>
        /// real entity
        /// </summary>
        public Ped MainPed { get; internal set; }
        /// <summary>
        /// The latest character health (may not have been applied yet)
        /// </summary>
        public int Health { get; internal set; }
        public bool _lastEnteringVehicle=false;
        public bool _lastSittingInVehicle=false;
        private bool _lastRagdoll=false;
        private ulong _lastRagdollTime=0;
        private bool _lastInCover = false;
        /// <summary>
        /// The latest character model hash (may not have been applied yet)
        /// </summary>
        public int ModelHash
        {
            get;set;
        }
        private Dictionary<byte, short> _lastClothes = null;
        public Dictionary<byte, short> Clothes { get; set; }

        public float Heading { get; set; }
        public Vector3 RotationVelocity { get; set; }
        public Vector3 AimCoords { get; set; }

        private WeaponAsset WeaponAsset { get; set; }
        

        public override void Update()
        {
            if (IsPlayer)
            {

                if (Username=="N/A")
                {
                    var p = PlayerList.GetPlayer(ID);
                    if (p!=null)
                    {
                        Username=p.Username;
                        if (PedBlip!=null)
                        {
                            PedBlip.Name=Username;
                        }
                    }

                }
                if((!API.Config.DisplayBlip) && (PedBlip!=null))
                {
                    PedBlip.Delete();
                    PedBlip=null;
                }
                RenderNameTag();
            }

            // Check if all data avalible
            if (!IsReady) { return; }

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate)
            {
                return;
            }


            bool characterExist = (MainPed != null) && MainPed.Exists();

            if (!characterExist)
            {
                CreateCharacter();
                return;
            }
            


            // Need to update state
            if (LastStateSynced>=LastUpdated)
            {
                
                if (MainPed!=null&& (ModelHash != MainPed.Model.Hash))
                {
                    CreateCharacter();
                    return;
                }

                if (!Clothes.Compare(_lastClothes))
                {
                    SetClothes();
                }

                CheckCurrentWeapon();
            }
            

            
            if (MainPed.IsDead)
            {
                if (Health>0)
                {
                    if (IsPlayer)
                    {
                        MainPed.Resurrect();
                    }
                    else
                    {
                        SyncEvents.TriggerPedKilled(this);
                    }
                }
            }
            else if (IsPlayer&&(MainPed.Health != Health))
            {
                MainPed.Health = Health;

                if (Health <= 0 && !MainPed.IsDead)
                {
                    MainPed.IsInvincible = false;
                    MainPed.Kill();
                    return;
                }
            }

            if (MainPed.IsInVehicle()||MainPed.IsGettingIntoVehicle)
            {
                DisplayInVehicle();
            }
            else
            {
                DisplayOnFoot();
            }
            LastUpdated=Main.Ticked;
            return;
        }

        
        private void RenderNameTag()
        {
            if (!API.Config.DisplayNameTag || (MainPed==null) || !MainPed.IsVisible || !MainPed.IsInRange(Game.Player.Character.Position, 20f))
            {
                return;
            }

            string renderText = IsOutOfSync ? "~r~AFK" : Username;
            Vector3 targetPos = MainPed.Bones[Bone.IKHead].Position + new Vector3(0, 0, 0.35f);

            Function.Call(Hash.SET_DRAW_ORIGIN, targetPos.X, targetPos.Y, targetPos.Z, 0);

            float dist = (GameplayCamera.Position - MainPed.Position).Length();
            var sizeOffset = Math.Max(1f - (dist / 30f), 0.3f);

            new ScaledText(new PointF(0, 0), renderText, 0.4f * sizeOffset, GTA.UI.Font.ChaletLondon)
            {
                Outline = true,
                Alignment = GTA.UI.Alignment.Center
            }.Draw();

            Function.Call(Hash.CLEAR_DRAW_ORIGIN);
        }

        private void CreateCharacter()
        {
            if (MainPed != null)
            {
                if (MainPed.Exists())
                {
                    Main.Logger.Debug($"Removing ped {ID}. Reason:CreateCharacter");
                    MainPed.Kill();
                    MainPed.MarkAsNoLongerNeeded();
                    MainPed.Delete();
                }
                
                MainPed = null;
            }

            if (PedBlip != null && PedBlip.Exists())
            {
                PedBlip.Delete();
                PedBlip = null;
            }

            Model characterModel = ModelHash.ModelRequest();
            if (characterModel == null)
            {
                return;
            }

            MainPed = World.CreatePed(characterModel, Position);
            characterModel.MarkAsNoLongerNeeded();
            if (MainPed == null)
            {
                return;
            }
            

            Function.Call(Hash.SET_PED_CAN_EVASIVE_DIVE, MainPed.Handle, false);
            Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD, MainPed.Handle, false);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, MainPed.Handle, true);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED_BY_PLAYER, MainPed.Handle, Game.Player, true);
            Function.Call(Hash.SET_PED_GET_OUT_UPSIDE_DOWN_VEHICLE, MainPed.Handle, false);
            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, MainPed.Handle, true, true);
            Function.Call(Hash._SET_PED_CAN_PLAY_INJURED_ANIMS, false);

            MainPed.BlockPermanentEvents = true;
            MainPed.CanWrithe=false;
            MainPed.CanBeDraggedOutOfVehicle = true;
            MainPed.IsOnlyDamagedByPlayer = false;
            MainPed.RelationshipGroup=Main.SyncedPedsGroup;
            MainPed.IsFireProof=false;
            MainPed.IsExplosionProof=false;

            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DrownsInWater,false);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableMelee, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableHurt, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableExplosionReactions, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_AvoidTearGas, false);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableShockingEvents, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableHurt, true);

            SetClothes();

            if (IsPlayer)
            {
                if (API.Config.DisplayBlip)
                {
                    // Add a new blip for the ped
                    PedBlip=MainPed.AddBlip();
                }
                MainPed.AttachedBlip.Color = BlipColor.White;
                MainPed.AttachedBlip.Scale = 0.8f;
                MainPed.AttachedBlip.Name =Username;

                MainPed.IsInvincible=true;
            }

            // Add to EntityPool so this Character can be accessed by handle.
            EntityPool.Add(this);
        }

        private void SetClothes()
        {
            foreach (KeyValuePair<byte, short> cloth in Clothes)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, MainPed.Handle, cloth.Key, cloth.Value, 0, 0);
            }
            _lastClothes = Clothes;
        }
        #region ONFOOT
        #region -- VARIABLES --
        /// <summary>
        /// The latest character rotation (may not have been applied yet)
        /// </summary>
        public byte Speed { get; set; }
        private bool _lastIsJumping = false;
        public bool IsJumping { get; set; }
        public bool IsOnLadder { get; set; }
        public bool IsVaulting { get; set; }
        public bool IsInParachuteFreeFall { get; set; }
        public bool IsParachuteOpen { get; set; }
        public Prop ParachuteProp { get; set; } = null;
        public bool IsRagdoll { get; set; }
        public bool IsOnFire { get; set; }
        public bool IsAiming { get; set; }
        public bool IsReloading { get; set; }
        public bool IsInCover { get; set; }
        public uint CurrentWeaponHash { get; set; }
        private Dictionary<uint, bool> _lastWeaponComponents = null;
        public Dictionary<uint, bool> WeaponComponents { get; set; } = null;
        private int _lastWeaponObj = 0;
        #endregion

        private string[] _currentAnimation = new string[2] { "", "" };

        private void DisplayOnFoot()
        {
            if (IsInParachuteFreeFall)
            {
                MainPed.PositionNoOffset = Vector3.Lerp(MainPed.Position, Position + Velocity, 0.5f);
                MainPed.Quaternion = Rotation.ToQuaternion();

                if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "skydive@base", "free_idle", 3))
                {
                    Function.Call(Hash.TASK_PLAY_ANIM, MainPed.Handle, LoadAnim("skydive@base"), "free_idle", 8f, 10f, -1, 0, -8f, 1, 1, 1);
                }
                return;
            }

            if (IsParachuteOpen)
            {
                if (ParachuteProp == null)
                {
                    Model model = 1740193300.ModelRequest();
                    if (model != null)
                    {
                        ParachuteProp = World.CreateProp(model, MainPed.Position, MainPed.Rotation, false, false);
                        model.MarkAsNoLongerNeeded();
                        ParachuteProp.IsPositionFrozen = true;
                        ParachuteProp.IsCollisionEnabled = false;

                        ParachuteProp.AttachTo(MainPed.Bones[Bone.SkelSpine2], new Vector3(3.6f, 0f, 0f), new Vector3(0f, 90f, 0f));
                    }
                    MainPed.Task.ClearAllImmediately();
                    MainPed.Task.ClearSecondary();
                }

                MainPed.PositionNoOffset = Vector3.Lerp(MainPed.Position, Position + Velocity, 0.5f);
                MainPed.Quaternion = Rotation.ToQuaternion();

                if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "skydive@parachute@first_person", "chute_idle_right", 3))
                {
                    Function.Call(Hash.TASK_PLAY_ANIM, MainPed, LoadAnim("skydive@parachute@first_person"), "chute_idle_right", 8f, 10f, -1, 0, -8f, 1, 1, 1);
                }

                return;
            }
            if (ParachuteProp != null)
            {
                if (ParachuteProp.Exists())
                {
                    ParachuteProp.Delete();
                }
                ParachuteProp = null;
            }

            if (IsOnLadder)
            {
                if (Velocity.Z < 0)
                {
                    string anim = Velocity.Z < -2f ? "slide_climb_down" : "climb_down";
                    if (_currentAnimation[1] != anim)
                    {
                        MainPed.Task.ClearAllImmediately();
                        _currentAnimation[1] = anim;
                    }

                    if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "laddersbase", anim, 3))
                    {
                        MainPed.Task.PlayAnimation("laddersbase", anim, 8f, -1, AnimationFlags.Loop);
                    }
                }
                else
                {
                    if (Math.Abs(Velocity.Z) < 0.5)
                    {
                        if (_currentAnimation[1] != "base_left_hand_up")
                        {
                            MainPed.Task.ClearAllImmediately();
                            _currentAnimation[1] = "base_left_hand_up";
                        }

                        if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "laddersbase", "base_left_hand_up", 3))
                        {
                            MainPed.Task.PlayAnimation("laddersbase", "base_left_hand_up", 8f, -1, AnimationFlags.Loop);
                        }
                    }
                    else
                    {
                        if (_currentAnimation[1] != "climb_up")
                        {
                            MainPed.Task.ClearAllImmediately();
                            _currentAnimation[1] = "climb_up";
                        }

                        if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "laddersbase", "climb_up", 3))
                        {
                            MainPed.Task.PlayAnimation("laddersbase", "climb_up", 8f, -1, AnimationFlags.Loop);
                        }
                    }
                }

                SmoothTransition();
                return;
            }
            if (!IsOnLadder && MainPed.IsTaskActive(TaskType.CTaskGoToAndClimbLadder))
            {
                MainPed.Task.ClearAllImmediately();
                _currentAnimation[1] = "";
            }

            if (IsVaulting)
            {
                if (!MainPed.IsVaulting)
                {
                    MainPed.Task.Climb();
                }

                SmoothTransition();

                return;
            }
            if (!IsVaulting && MainPed.IsVaulting)
            {
                MainPed.Task.ClearAllImmediately();
            }

            if (IsOnFire && !MainPed.IsOnFire)
            {

                MainPed.SetOnFire(true);
            }
            else if (!IsOnFire && MainPed.IsOnFire)
            {
                MainPed.SetOnFire(false);
            }

            if (IsJumping)
            {
                if (!_lastIsJumping)
                {
                    _lastIsJumping = true;
                    MainPed.Task.Jump();
                }

                SmoothTransition();
                return;
            }
            _lastIsJumping = false;

            if (IsRagdoll || Health==0)
            {
                if (!MainPed.IsRagdoll)
                {
                    MainPed.Ragdoll();
                }
                
                SmoothTransition();
                if (!_lastRagdoll)
                {
                    _lastRagdoll = true;
                    _lastRagdollTime=Main.Ticked;
                }
                /*
                if((Main.Ticked-_lastRagdollTime>30)&&((Position.DistanceTo(MainPed.Position)>2)||MainPed.Velocity.Length()<3f))
                {
                    MainPed.ApplyForce((Position-MainPed.Position)*0.2f, (RotationVelocity-MainPed.RotationVelocity)*0.1f);

                }*/
                return;
            }
            else
            {
                if (MainPed.IsRagdoll)
                {
                    if (Speed==0)
                    {
                        MainPed.CancelRagdoll();
                    }
                    else
                    {
                        MainPed.Task.ClearAllImmediately();
                    }
                    return;
                }
                else
                {
                    _lastRagdoll = false;
                }
            }


            if (IsReloading)
            {
                if (!MainPed.IsTaskActive(TaskType.CTaskReloadGun))
                {
                    MainPed.Task.ReloadWeapon();
                }
                /*
                if (!_isPlayingAnimation)
                {
                    string[] reloadingAnim = MainPed.GetReloadingAnimation();
                    if (reloadingAnim != null)
                    {
                        _isPlayingAnimation = true;
                        _currentAnimation = reloadingAnim;
                        MainPed.Task.PlayAnimation(_currentAnimation[0], _currentAnimation[1], 8f, -1, AnimationFlags.AllowRotation | AnimationFlags.UpperBodyOnly);
                    }
                }
                */
                SmoothTransition();
            }
            else if (IsInCover)
            {

                
                if (!_lastInCover)
                {
                    Function.Call(Hash.TASK_STAY_IN_COVER, MainPed.Handle);
                }

                _lastInCover=true;
                if (IsAiming)
                {
                    DisplayAiming();
                    _lastInCover=false;
                }
                else if (MainPed.IsInCover)
                {
                    SmoothTransition();
                }
                return;
            }
            else if (_lastInCover)
            {
                MainPed.Task.ClearAllImmediately();
                _lastInCover=false;
            }
            else if (IsAiming)
            {
                DisplayAiming();
            }
            else if (MainPed.IsShooting)
            {
                MainPed.Task.ClearAllImmediately();
            }
            else
            {
                WalkTo();
            }
        }

        #region WEAPON
        private void CheckCurrentWeapon()
        {
            if (MainPed.Weapons.Current.Hash != (WeaponHash)CurrentWeaponHash || !WeaponComponents.Compare(_lastWeaponComponents))
            {
                if (WeaponAsset!=null) { WeaponAsset.MarkAsNoLongerNeeded(); }
                WeaponAsset=new WeaponAsset(CurrentWeaponHash);
                if (!WeaponAsset.IsLoaded) { WeaponAsset.Request(); }
                MainPed.Weapons.RemoveAll();
                _lastWeaponObj = Function.Call<int>(Hash.CREATE_WEAPON_OBJECT, CurrentWeaponHash, -1, Position.X, Position.Y, Position.Z, true, 0, 0);

                if (CurrentWeaponHash != (uint)WeaponHash.Unarmed)
                {
                    if (WeaponComponents != null && WeaponComponents.Count != 0)
                    {
                        foreach (KeyValuePair<uint, bool> comp in WeaponComponents)
                        {

                            if (comp.Value)
                            {
                                Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_WEAPON_OBJECT, _lastWeaponObj, comp.Key);
                            }

                        }

                    }
                    Function.Call(Hash.GIVE_WEAPON_OBJECT_TO_PED, _lastWeaponObj, MainPed.Handle);

                }

                _lastWeaponComponents = WeaponComponents;
            }
        }

        private void DisplayAiming()
        {
            if (Velocity==default)
            {
                MainPed.Task.AimAt(AimCoords,1000);
            }
            else
            {
                Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, MainPed.Handle,
                Position.X+Velocity.X, Position.Y+Velocity.Y, Position.Z+Velocity.Z,
                AimCoords.X, AimCoords.Y, AimCoords.Z, 3f, false, 0x3F000000, 0x40800000, false, 512, false, 0);

            }
            SmoothTransition();
        }
        #endregion

        private bool LastMoving;
        private void WalkTo()
        {
            Vector3 predictPosition = Position + (Position - MainPed.Position) + Velocity * 0.5f;
            float range = predictPosition.DistanceToSquared(MainPed.Position);

            switch (Speed)
            {
                case 1:
                    if (!MainPed.IsWalking || range > 0.25f)
                    {
                        float nrange = range * 2;
                        if (nrange > 1.0f)
                        {
                            nrange = 1.0f;
                        }

                        MainPed.Task.GoStraightTo(predictPosition);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, MainPed.Handle, nrange);
                    }
                    LastMoving = true;
                    break;
                case 2:
                    if (!MainPed.IsRunning || range > 0.50f)
                    {
                        MainPed.Task.RunTo(predictPosition, true);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, MainPed.Handle, 1.0f);
                    }
                    LastMoving = true;
                    break;
                case 3:
                    if (!MainPed.IsSprinting || range > 0.75f)
                    {
                        Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, MainPed.Handle, predictPosition.X, predictPosition.Y, predictPosition.Z, 3.0f, -1, 0.0f, 0.0f);
                        Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, MainPed.Handle, 1.49f);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, MainPed.Handle, 1.0f);
                    }
                    LastMoving = true;
                    break;
                default:
                    if (LastMoving)
                    {
                        MainPed.Task.StandStill(2000);
                        LastMoving = false;
                    }
                    break;
            }
            SmoothTransition();
        }

        private void SmoothTransition()
        {
            var localRagdoll = MainPed.IsRagdoll;
            var dist = Position.DistanceTo(MainPed.Position);
            if (dist>3)
            {
                MainPed.PositionNoOffset=Position;
                return;
            }
            var f = dist*(Position+SyncParameters.PositioinPredictionDefault*Velocity-MainPed.Position)+(Velocity-MainPed.Velocity)*0.2f;
            if (!localRagdoll) { f*=5; }
            if (!(localRagdoll|| MainPed.IsDead))
            {
                MainPed.Rotation=Rotation;
                if (MainPed.Speed<0.05) { f*=10; MainPed.Heading=Heading; }
            }
            else if (Main.Ticked-_lastRagdollTime<10)
            {
                return;
            }
            MainPed.ApplyForce(f);
        }
        private string LoadAnim(string anim)
        {
            ulong startTime = Util.GetTickCount64();

            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, anim))
            {
                Script.Yield();
                Function.Call(Hash.REQUEST_ANIM_DICT, anim);
                if (Util.GetTickCount64() - startTime >= 1000)
                {
                    break;
                }
            }

            return anim;
        }
        #endregion

        private void DisplayInVehicle()
        {
            if (MainPed.IsOnTurretSeat())
            {
                Function.Call(Hash.SET_VEHICLE_TURRET_SPEED_THIS_FRAME, MainPed.CurrentVehicle, 100);
                Function.Call(Hash.TASK_VEHICLE_AIM_AT_COORD, MainPed.Handle, AimCoords.X, AimCoords.Y, AimCoords.Z);
            }
        }
    }
}
