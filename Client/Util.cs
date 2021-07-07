using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

using GTA;
using GTA.Native;
using GTA.Math;

namespace CoopClient
{
    class Util
    {
        public static byte GetPedSpeed(Ped ped)
        {
            if (ped.IsWalking)
            {
                return 1;
            }
            else if (ped.IsRunning)
            {
                return 2;
            }
            else if (ped.IsSprinting)
            {
                return 3;
            }
            return 0;
        }

        public static Vector3 GetPedAimCoords(Ped ped, bool isNpc)
        {
            bool aimOrShoot = ped.IsAiming || ped.IsShooting && ped.Weapons.Current?.AmmoInClip != 0;
            return aimOrShoot ? (isNpc ? GetLastWeaponImpact(ped) : RaycastEverything(new Vector2(0, 0))) : new Vector3();
        }

        public static byte? GetPedFlags(Ped ped, bool fullSync)
        {
            byte? flags = 0;

            if (fullSync)
            {
                flags |= (byte)PedDataFlags.LastSyncWasFull;
            }

            if (ped.IsAiming)
            {
                flags |= (byte)PedDataFlags.IsAiming;
            }

            if (ped.IsShooting && ped.Weapons.Current?.AmmoInClip != 0)
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

        public static Dictionary<int, int> GetPedProps(Ped ped)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            for (int i = 0; i < 11; i++)
            {
                int mod = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, ped.Handle, i);
                result.Add(i, mod);
            }
            return result;
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

                using (FileStream stream = new FileStream(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
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
                (IntersectFlags)(1 | 16 | 256 | 2 | 4 | 8), // | peds + vehicles
                ignoreEntity);

            if (raycastResults.DidHit)
            {
                return raycastResults.HitPosition;
            }

            return camPos + dir * raycastToDist;
        }
    }
}
