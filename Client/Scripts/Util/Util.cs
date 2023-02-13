using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI.Elements;
using Newtonsoft.Json;
using RageCoop.Core;
using static RageCoop.Client.Shared;
using Font = GTA.UI.Font;

[assembly: InternalsVisibleTo("RageCoop.Client.Installer")]

namespace RageCoop.Client
{
    internal static class Util
    {
        /// <summary>
        ///     The location of the cursor on screen between 0 and 1.
        /// </summary>
        public static PointF CursorPositionRelative
        {
            get
            {
                var cursorX = Game.IsControlEnabled(Control.CursorX)
                    ? Game.GetControlValueNormalized(Control.CursorX)
                    : Game.GetDisabledControlValueNormalized(Control.CursorX);
                var cursorY = Game.IsControlEnabled(Control.CursorY)
                    ? Game.GetControlValueNormalized(Control.CursorY)
                    : Game.GetDisabledControlValueNormalized(Control.CursorY);
                return new PointF(cursorX, cursorY);
            }
        }

        public static Point CursorPosition
        {
            get
            {
                var p = CursorPositionRelative;
                var res = Screen.Resolution;
                return new Point((int)(p.X * res.Width), (int)(p.Y * res.Height));
            }
        }

        public static SizeF ResolutionMaintainRatio
        {
            get
            {
                // Get the game width and height
                var screenw = Screen.Resolution.Width;
                var screenh = Screen.Resolution.Height;
                // Calculate the ratio
                var ratio = (float)screenw / screenh;
                // And the width with that ratio
                var width = 1080f * ratio;
                // Finally, return a SizeF
                return new SizeF(width, 1080f);
            }
        }

        public static Vector3 GetRotation(this EntityBone b)
        {
            return b.ForwardVector.ToEulerRotation(b.UpVector);
        }

        public static void DrawTextFromCoord(Vector3 coord, string text, float scale = 0.5f, Point offset = default)
        {
            Point toDraw = default;
            if (WorldToScreen(coord, ref toDraw))
            {
                toDraw.X += offset.X;
                toDraw.Y += offset.Y;
                new ScaledText(toDraw, text, scale, Font.ChaletLondon)
                {
                    Outline = true,
                    Alignment = Alignment.Center,
                    Color = Color.White
                }.Draw();
            }
        }

        public static bool WorldToScreen(Vector3 pos, ref Point screenPos)
        {
            float x, y;
            unsafe
            {
                var res = ResolutionMaintainRatio;
                if (Call<bool>(GET_SCREEN_COORD_FROM_WORLD_COORD, pos.X, pos.Y, pos.Z, &x, &y))
                {
                    screenPos = new Point((int)(res.Width * x), (int)(y * 1080));
                    return true;
                }
            }

            return false;
        }

        public static Settings ReadSettings(string path = null)
        {
            path = path ?? SettingsPath;

            Directory.CreateDirectory(Directory.GetParent(path).FullName);
            Settings settings;
            try
            {
                settings = JsonDeserialize<Settings>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Log?.Error(ex);
                File.WriteAllText(path, JsonSerialize(settings = new Settings()));
            }

            return settings;
        }

        public static bool SaveSettings(string path = null, Settings settings = null)
        {
            try
            {
                path = path ?? SettingsPath;
                settings = settings ?? Main.Settings;
                Directory.CreateDirectory(Directory.GetParent(path).FullName);

                File.WriteAllText(path, JsonSerialize(settings));
                return true;
            }
            catch (Exception ex)
            {
                Log?.Error(ex);
                return false;
            }
        }


        public static Vehicle CreateVehicle(Model model, Vector3 position, float heading = 0f)
        {
            if (!model.IsLoaded) return null;
            return (Vehicle)Entity.FromHandle(Call<int>(CREATE_VEHICLE, model.Hash, position.X,
                position.Y, position.Z, heading, false, false));
        }

        public static Ped CreatePed(Model model, Vector3 position, float heading = 0f)
        {
            if (!model.IsLoaded) return null;
            return (Ped)Entity.FromHandle(Call<int>(CREATE_PED, 26, model.Hash, position.X, position.Y,
                position.Z, heading, false, false));
        }

        public static void SetOnFire(this Entity e, bool toggle)
        {
            if (toggle)
                Call(START_ENTITY_FIRE, e.Handle);
            else
                Call(STOP_ENTITY_FIRE, e.Handle);
        }

        public static void SetFrozen(this Entity e, bool toggle)
        {
            Call(FREEZE_ENTITY_POSITION, e, toggle);
        }

        public static SyncedPed GetSyncEntity(this Ped p)
        {
            if (p == null) return null;
            var c = EntityPool.GetPedByHandle(p.Handle);
            if (c == null) EntityPool.Add(c = new SyncedPed(p));
            return c;
        }

        public static SyncedVehicle GetSyncEntity(this Vehicle veh)
        {
            if (veh == null) return null;
            var v = EntityPool.GetVehicleByHandle(veh.Handle);
            if (v == null) EntityPool.Add(v = new SyncedVehicle(veh));
            return v;
        }

        public static void ApplyForce(this Entity e, int boneIndex, Vector3 direction, Vector3 rotation = default,
            ForceType forceType = ForceType.MaxForceRot2)
        {
            Call(APPLY_FORCE_TO_ENTITY, e.Handle, forceType, direction.X, direction.Y, direction.Z,
                rotation.X, rotation.Y, rotation.Z, boneIndex, false, true, true, false, true);
        }

        public static byte GetPlayerRadioIndex()
        {
            return (byte)Call<int>(GET_PLAYER_RADIO_STATION_INDEX);
        }

        public static void SetPlayerRadioIndex(int index)
        {
            Call(SET_RADIO_TO_STATION_INDEX, index);
        }

        public static EntityPopulationType GetPopulationType(int handle)
            => (EntityPopulationType)Call<int>(GET_ENTITY_POPULATION_TYPE, handle);

        public static unsafe void DeleteEntity(int handle)
        {
            Call(SET_ENTITY_AS_MISSION_ENTITY, handle, false, true);
            Call(DELETE_ENTITY, &handle);
        }

        [DllImport("kernel32.dll")]
        public static extern ulong GetTickCount64();


        #region -- POINTER --

        private static int _steeringAngleOffset { get; set; }

        public static unsafe void NativeMemory()
        {
            IntPtr address;

            address = Game.FindPattern("\x74\x0A\xF3\x0F\x11\xB3\x1C\x09\x00\x00\xEB\x25", "xxxxxx????xx");
            if (address != IntPtr.Zero) _steeringAngleOffset = *(int*)(address + 6) + 8;
        }

        public static unsafe void CustomSteeringAngle(this Vehicle veh, float value)
        {
            var address = new IntPtr((long)veh.MemoryAddress);
            if (address == IntPtr.Zero || _steeringAngleOffset == 0) return;

            *(float*)(address + _steeringAngleOffset).ToPointer() = value;
        }

        #endregion

        #region MATH

        public static Vector3 LinearVectorLerp(Vector3 start, Vector3 end, ulong currentTime, int duration)
        {
            return new Vector3
            {
                X = LinearFloatLerp(start.X, end.X, currentTime, duration),
                Y = LinearFloatLerp(start.Y, end.Y, currentTime, duration),
                Z = LinearFloatLerp(start.Z, end.Z, currentTime, duration)
            };
        }

        public static float LinearFloatLerp(float start, float end, ulong currentTime, int duration)
        {
            return (end - start) * currentTime / duration + start;
        }

        public static float Lerp(float from, float to, float fAlpha)
        {
            return from * (1.0f - fAlpha) + to * fAlpha; //from + (to - from) * fAlpha
        }

        public static Vector3 RotationToDirection(Vector3 rotation)
        {
            var z = MathExtensions.DegToRad(rotation.Z);
            var x = MathExtensions.DegToRad(rotation.X);
            var num = Math.Abs(Math.Cos(x));

            return new Vector3
            {
                X = (float)(-Math.Sin(z) * num),
                Y = (float)(Math.Cos(z) * num),
                Z = (float)Math.Sin(x)
            };
        }

        #endregion
    }
}