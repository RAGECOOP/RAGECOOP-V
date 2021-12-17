using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using GTA;
using GTA.Native;
using GTA.Math;

namespace CoopClient
{
    static class Util
    {
        #region -- POINTER --
        private static int SteeringAngleOffset { get; set; }

        public static unsafe void NativeMemory()
        {
            IntPtr address;

            address = Game.FindPattern("\x74\x0A\xF3\x0F\x11\xB3\x1C\x09\x00\x00\xEB\x25", "xxxxxx????xx");
            if (address != IntPtr.Zero)
            {
                SteeringAngleOffset = *(int*)(address + 6) + 8;
            }

            address = Game.FindPattern("\x32\xc0\xf3\x0f\x11\x09", "xxxxxx"); // Weapon / Radio slowdown
            if (address != IntPtr.Zero)
            {
                for (int i = 0; i < 6; i++)
                {
                    *(byte*)(address + i).ToPointer() = 0x90;
                }
            }
        }

        public static unsafe void CustomSteeringAngle(this Vehicle veh, float value)
        {
            IntPtr address = new IntPtr((long)veh.MemoryAddress);
            if (address == IntPtr.Zero || SteeringAngleOffset == 0)
            {
                return;
            }

            *(float*)(address + SteeringAngleOffset).ToPointer() = value;
        }
        #endregion

        public static T GetGameMs<T>() where T : IConvertible
        {
            return (T)Convert.ChangeType(1f / (Game.FPS > 60f ? 60f : Game.FPS) * 1000f, typeof(T));
        }

        public static Model ModelRequest(this int hash)
        {
            Model model = new Model(hash);

            if (!model.IsValid)
            {
                //GTA.UI.Notification.Show("~y~Not valid!");
                return null;
            }

            if (!model.IsLoaded)
            {
                return model.Request(1000) ? model : null;
            }

            return model;
        }

        public static bool IsBetween<T>(this T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0 && Comparer<T>.Default.Compare(item, end) <= 0;
        }

        public static bool Compare<T, Y>(this Dictionary<T, Y> item, Dictionary<T, Y> item2)
        {
            if (item == null || item2 == null || item.Count != item2.Count)
            {
                return false;
            }

            foreach (KeyValuePair<T, Y> pair in item)
            {
                if (item2.TryGetValue(pair.Key, out Y value) && Equals(value, pair.Value))
                {
                    continue;
                }

                // TryGetValue() or Equals failed
                return false;
            }

            // No difference between item and item2
            return true;
        }

        public static Vector3 LinearVectorLerp(Vector3 start, Vector3 end, ulong currentTime, int duration)
        {
            return new Vector3()
            {
                X = LinearFloatLerp(start.X, end.X, currentTime, duration),
                Y = LinearFloatLerp(start.Y, end.Y, currentTime, duration),
                Z = LinearFloatLerp(start.Z, end.Z, currentTime, duration),
            };
        }

        public static float LinearFloatLerp(float start, float end, ulong currentTime, int duration)
        {
            return (end - start) * currentTime / duration + start;
        }

        public static int GetResponsiblePedHandle(this Vehicle veh)
        {
            if (veh == null || veh.Handle == 0 || !veh.Exists())
            {
                return 0;
            }

            if (!veh.IsSeatFree(VehicleSeat.Driver))
            {
                return veh.GetPedOnSeat(VehicleSeat.Driver).Handle;
            }

            for (int i = 0; i < veh.PassengerCapacity; i++)
            {
                if (!veh.IsSeatFree((VehicleSeat)i))
                {
                    return veh.GetPedOnSeat((VehicleSeat)i).Handle;
                }
            }

            return 0;
        }

        public static byte GetPedSpeed(this Ped ped)
        {
            if (ped.IsSprinting)
            {
                return 3;
            }
            if (ped.IsRunning)
            {
                return 2;
            }
            if (ped.IsWalking)
            {
                return 1;
            }
            
            return 0;
        }

        public static Vector3 GetPedAimCoords(this Ped ped, bool isNpc)
        {
            bool aimOrShoot = ped.IsAiming || ped.IsShooting && ped.Weapons.Current?.AmmoInClip != 0;
            return aimOrShoot ? (isNpc ? GetLastWeaponImpact(ped) : RaycastEverything(new Vector2(0, 0))) : new Vector3();
        }

        /// <summary>
        /// Only works for players NOT NPCs
        /// </summary>
        /// <returns>Vector3</returns>
        public static Vector3 GetVehicleAimCoords()
        {
            return RaycastEverything(new Vector2(0, 0));
        }

        public static byte? GetVehicleFlags(this Vehicle veh)
        {
            byte? flags = 0;

            if (veh.IsEngineRunning)
            {
                flags |= (byte)VehicleDataFlags.IsEngineRunning;
            }

            if (veh.AreLightsOn)
            {
                flags |= (byte)VehicleDataFlags.AreLightsOn;
            }

            if (veh.AreHighBeamsOn)
            {
                flags |= (byte)VehicleDataFlags.AreHighBeamsOn;
            }

            if (veh.IsSirenActive)
            {
                flags |= (byte)VehicleDataFlags.IsSirenActive;
            }

            if (veh.IsDead)
            {
                flags |= (byte)VehicleDataFlags.IsDead;
            }

            if (Function.Call<bool>(Hash.IS_HORN_ACTIVE, veh.Handle))
            {
                flags |= (byte)VehicleDataFlags.IsHornActive;
            }

            if (veh.IsSubmarineCar && Function.Call<bool>(Hash._GET_IS_SUBMARINE_VEHICLE_TRANSFORMED, veh))
            {
                flags |= (byte)VehicleDataFlags.IsTransformed;
            }

            return flags;
        }

        public static byte? GetPedFlags(this Ped ped, bool isPlayer = false)
        {
            byte? flags = 0;

            if (ped.IsAiming)
            {
                flags |= (byte)PedDataFlags.IsAiming;
            }

            if ((ped.IsShooting || isPlayer && Game.IsControlPressed(Control.Attack)) && ped.Weapons.Current?.AmmoInClip != 0)
            {
                flags |= (byte)PedDataFlags.IsShooting;
            }

            if (ped.IsReloading)
            {
                flags |= (byte)PedDataFlags.IsReloading;
            }

            if (ped.IsJumping)
            {
                flags |= (byte)PedDataFlags.IsJumping;
            }

            if (ped.IsRagdoll)
            {
                flags |= (byte)PedDataFlags.IsRagdoll;
            }

            if (ped.IsOnFire)
            {
                flags |= (byte)PedDataFlags.IsOnFire;
            }

            return flags;
        }

        public static Dictionary<byte, short> GetPedClothes(this Ped ped)
        {
            Dictionary<byte, short> result = new Dictionary<byte, short>();
            for (byte i = 0; i < 11; i++)
            {
                short mod = Function.Call<short>(Hash.GET_PED_DRAWABLE_VARIATION, ped.Handle, i);
                result.Add(i, mod);
            }
            return result;
        }

        public static Dictionary<uint, bool> GetWeaponComponents(this Weapon weapon)
        {
            Dictionary<uint, bool> result = null;

            if (weapon.Components.Count > 0)
            {
                result = new Dictionary<uint, bool>();

                foreach (var comp in weapon.Components)
                {
                    result.Add((uint)comp.ComponentHash, comp.Active);
                }
            }

            return result;
        }

        public static Dictionary<int, int> GetVehicleMods(this VehicleModCollection mods)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            foreach (VehicleMod mod in mods.ToArray())
            {
                result.Add((int)mod.Type, mod.Index);
            }
            return result;
        }

        public static VehicleDoors[] GetVehicleDoors(this VehicleDoorCollection doors)
        {
            int doorLength = doors.ToArray().Length;
            if (doorLength == 0)
            {
                return null;
            }

            VehicleDoors[] result = new VehicleDoors[doorLength];
            for (int i = 0; i < (doorLength - 1); i++)
            {
                VehicleDoors currentDoor = new VehicleDoors()
                {
                    AngleRatio = doors[(VehicleDoorIndex)i].AngleRatio,
                    Broken = doors[(VehicleDoorIndex)i].IsBroken,
                    Open = doors[(VehicleDoorIndex)i].IsOpen,
                    FullyOpen = doors[(VehicleDoorIndex)i].IsFullyOpen
                };
                result[i] = currentDoor;
            }

            return result;
        }

        public static int GetBurstedTires(this VehicleWheelCollection wheels)
        {
            int tireFlag = 0;

            foreach (var wheel in wheels.GetAllWheels())
            {
                if (wheel.IsBursted)
                {
                    tireFlag |= (1 << (int)wheel.BoneId);
                }
            }

            return tireFlag;
        }

        public static Settings ReadSettings()
        {
            XmlSerializer ser = new XmlSerializer(typeof(Settings));

            string path = Directory.GetCurrentDirectory() + "\\scripts\\CoopSettings.xml";
            Settings settings = null;

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    settings = (Settings)ser.Deserialize(stream);
                }

                using (FileStream stream = new FileStream(path, FileMode.Truncate, FileAccess.ReadWrite))
                {
                    ser.Serialize(stream, settings);
                }
            }
            else
            {
                using (FileStream stream = File.OpenWrite(path))
                {
                    ser.Serialize(stream, settings = new Settings());
                }
            }

            return settings;
        }

        public static void SaveSettings()
        {
            try
            {
                string path = Directory.GetCurrentDirectory() + "\\scripts\\CoopSettings.xml";

                using (FileStream stream = new FileStream(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Settings));
                    ser.Serialize(stream, Main.MainSettings);
                }
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show("Error saving player settings: " + ex.Message);
            }
        }

        public static Vector3 GetLastWeaponImpact(Ped ped)
        {
            OutputArgument coord = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_PED_LAST_WEAPON_IMPACT_COORD, ped.Handle, coord))
            {
                return new Vector3();
            }

            return coord.GetResult<Vector3>();
        }

        public static bool IsTurretSeat(this Vehicle veh, int seat)
        {
            if (!Function.Call<bool>(Hash.DOES_VEHICLE_HAVE_WEAPONS, veh.Handle))
            {
                return false;
            }

            switch (seat)
            {
                case -1:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Rhino
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Khanjari
                        || (VehicleHash)veh.Model.Hash == VehicleHash.FireTruck
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Riot2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Cerberus
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Cerberus2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Cerberus3;
                case 0:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Apc;
                case 1:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Technical
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Technical2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Technical3
                        || (VehicleHash)veh.Model.Hash == VehicleHash.HalfTrack
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Barrage;
                case 2:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Valkyrie2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Barrage;
                case 3:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Limo2
                        || (VehicleHash)veh.Model.Hash == VehicleHash.Dinghy5;
                case 7:
                    return (VehicleHash)veh.Model.Hash == VehicleHash.Insurgent;
            }

            return false;
        }

        public static double DegToRad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        public static Vector3 RotationToDirection(Vector3 rotation)
        {
            double z = DegToRad(rotation.Z);
            double x = DegToRad(rotation.X);
            double num = Math.Abs(Math.Cos(x));

            return new Vector3
            {
                X = (float)(-Math.Sin(z) * num),
                Y = (float)(Math.Cos(z) * num),
                Z = (float)Math.Sin(x)
            };
        }

        public static bool WorldToScreenRel(Vector3 worldCoords, out Vector2 screenCoords)
        {
            OutputArgument num1 = new OutputArgument();
            OutputArgument num2 = new OutputArgument();

            if (!Function.Call<bool>(Hash.GET_SCREEN_COORD_FROM_WORLD_COORD, worldCoords.X, worldCoords.Y, worldCoords.Z, num1, num2))
            {
                screenCoords = new Vector2();
                return false;
            }

            screenCoords = new Vector2((num1.GetResult<float>() - 0.5f) * 2, (num2.GetResult<float>() - 0.5f) * 2);
            return true;
        }

        public static Vector3 ScreenRelToWorld(Vector3 camPos, Vector3 camRot, Vector2 coord)
        {
            Vector3 camForward = RotationToDirection(camRot);
            Vector3 rotUp = camRot + new Vector3(10, 0, 0);
            Vector3 rotDown = camRot + new Vector3(-10, 0, 0);
            Vector3 rotLeft = camRot + new Vector3(0, 0, -10);
            Vector3 rotRight = camRot + new Vector3(0, 0, 10);

            Vector3 camRight = RotationToDirection(rotRight) - RotationToDirection(rotLeft);
            Vector3 camUp = RotationToDirection(rotUp) - RotationToDirection(rotDown);

            double rollRad = -DegToRad(camRot.Y);

            Vector3 camRightRoll = camRight * (float)Math.Cos(rollRad) - camUp * (float)Math.Sin(rollRad);
            Vector3 camUpRoll = camRight * (float)Math.Sin(rollRad) + camUp * (float)Math.Cos(rollRad);

            Vector3 point3D = camPos + camForward * 10.0f + camRightRoll + camUpRoll;
            if (!WorldToScreenRel(point3D, out Vector2 point2D))
            {
                return camPos + camForward * 10.0f;
            }

            Vector3 point3DZero = camPos + camForward * 10.0f;
            if (!WorldToScreenRel(point3DZero, out Vector2 point2DZero))
            {
                return camPos + camForward * 10.0f;
            }

            const double eps = 0.001;
            if (Math.Abs(point2D.X - point2DZero.X) < eps || Math.Abs(point2D.Y - point2DZero.Y) < eps)
            {
                return camPos + camForward * 10.0f;
            }

            float scaleX = (coord.X - point2DZero.X) / (point2D.X - point2DZero.X);
            float scaleY = (coord.Y - point2DZero.Y) / (point2D.Y - point2DZero.Y);

            return camPos + camForward * 10.0f + camRightRoll * scaleX + camUpRoll * scaleY;
        }

        public static Vector3 RaycastEverything(Vector2 screenCoord)
        {
            Vector3 camPos = GameplayCamera.Position;
            Vector3 camRot = GameplayCamera.Rotation;
            const float raycastToDist = 100.0f;
            const float raycastFromDist = 1f;

            Vector3 target3D = ScreenRelToWorld(camPos, camRot, screenCoord);
            Vector3 source3D = camPos;

            Entity ignoreEntity = Game.Player.Character;
            if (Game.Player.Character.IsInVehicle())
            {
                ignoreEntity = Game.Player.Character.CurrentVehicle;
            }

            Vector3 dir = target3D - source3D;
            dir.Normalize();
            RaycastResult raycastResults = World.Raycast(source3D + dir * raycastFromDist,
                source3D + dir * raycastToDist,
                IntersectFlags.Everything,
                ignoreEntity);

            if (raycastResults.DidHit)
            {
                return raycastResults.HitPosition;
            }

            return camPos + dir * raycastToDist;
        }

        [DllImport("kernel32.dll")]
        public static extern ulong GetTickCount64();
    }
}
