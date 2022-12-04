using System;
using System.Drawing;
using GTA;
using RageCoop.Core;

namespace RageCoop.Client
{
    [ScriptAttributes(Author = "RageCoop", NoDefaultInstance = false,
        SupportURL = "https://github.com/RAGECOOP/RAGECOOP-V")]
    internal class DevTool : Script
    {
        public static Vehicle ToMark;
        public static Script Instance;

        public DevTool()
        {
            Util.StartUpCheck();
            Instance = this;
            Tick += OnTick;
            Pause();
        }

        private void OnTick(object sender, EventArgs e)
        {
            foreach (var p in World.GetAllPeds()) DrawWeaponBone(p);
            if (ToMark == null) return;

            if (WeaponUtil.VehicleWeapons.TryGetValue((uint)(int)ToMark.Model, out var info))
                foreach (var ws in info.Weapons)
                    foreach (var w in ws.Value.Bones)
                        DrawBone(w.BoneName, ws.Value.Name + ":" + ws.Key.ToHex());
            var P = Game.Player.Character;
            var b = ToMark.GetMuzzleBone(P.VehicleWeapon);
            if (b != null) World.DrawLine(b.Position, b.Position + b.ForwardVector * 5, Color.Brown);
        }

        public static void DrawWeaponBone(Ped p)
        {
            var wb = p.Weapons?.CurrentWeaponObject?.Bones["gun_muzzle"];
            if (wb?.IsValid == true) World.DrawLine(wb.Position, wb.Position + wb.RightVector, Color.Blue);
            if (wb?.IsValid == true) World.DrawLine(wb.Position, wb.Position + wb.ForwardVector, Color.Red);
            if (wb?.IsValid == true) World.DrawLine(wb.Position, wb.Position + wb.UpVector, Color.Green);

        }
        private void FindAndDraw()
        {
            DrawBone("weapon_1a");
            DrawBone("weapon_1b");
            DrawBone("weapon_1c");
            DrawBone("weapon_1d");
            DrawBone("weapon_2a");
            DrawBone("weapon_2b");
            DrawBone("weapon_2c");
            DrawBone("weapon_2d");
            DrawBone("weapon_3a");
            DrawBone("weapon_3b");
            DrawBone("weapon_3c");
            DrawBone("weapon_3d");
            DrawBone("weapon_4a");
            DrawBone("weapon_4b");
            DrawBone("weapon_4c");
            DrawBone("weapon_4d");
            DrawBone("weapon_1e");
            DrawBone("weapon_1f");
            DrawBone("weapon_1g");
            DrawBone("weapon_1h");
            DrawBone("weapon_2e");
            DrawBone("weapon_2f");
            DrawBone("weapon_2g");
            DrawBone("weapon_2h");
            DrawBone("weapon_3e");
            DrawBone("weapon_3f");
            DrawBone("weapon_3g");
            DrawBone("weapon_3h");
            DrawBone("weapon_4e");
            DrawBone("weapon_4f");
            DrawBone("weapon_4g");
            DrawBone("weapon_4h");
        }

        private void DrawBone(string name, string text = null)
        {
            text = text ?? name;
            var b = ToMark.Bones[name];
            if (b.IsValid)
            {
                var start = b.Position;
                var end = b.Position + b.ForwardVector * 5;
                World.DrawLine(start, end, Color.AliceBlue);
                Util.DrawTextFromCoord(end, text, 0.35f);
            }
        }
    }
}