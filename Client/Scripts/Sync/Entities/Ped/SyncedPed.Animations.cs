using GTA;
using GTA.Native;

namespace RageCoop.Client
{
    public partial class SyncedPed
    {
        private void DisplaySpeaking(bool speaking)
        {
            if (!MainPed.IsHuman)
                return;

            if (speaking)
            {
                Call(PLAY_FACIAL_ANIM, MainPed.Handle, "mic_chatter", "mp_facial");
                return;
            }

            switch (MainPed.Gender)
            {
                case Gender.Male:
                    Call(PLAY_FACIAL_ANIM, MainPed.Handle, "mood_normal_1",
                        "facials@gen_male@variations@normal");
                    break;
                case Gender.Female:
                    Call(PLAY_FACIAL_ANIM, MainPed.Handle, "mood_normal_1",
                        "facials@gen_female@variations@normal");
                    break;
                default:
                    Call(PLAY_FACIAL_ANIM, MainPed.Handle, "mood_normal_1",
                        "facials@mime@variations@normal");
                    break;
            }
        }

        private void DisplayInCover()
        {
            var ourAnim = GetCoverAnim();
            var animDict = GetCoverIdleAnimDict();

            if (ourAnim != null && animDict != null)
            {
                var flag = AnimationFlags.Loop;
                if (!Call<bool>(IS_ENTITY_PLAYING_ANIM, MainPed, animDict, ourAnim, 3))
                {
                    if (LoadAnim(animDict) == null) return;

                    MainPed.Task.ClearAll();
                    Call(TASK_PLAY_ANIM, MainPed, animDict, ourAnim, 8f, 10f, -1, flag, -8f, 1, 1, 1);
                }
            }
        }

        internal string GetCoverAnim()
        {
            if (IsInCover)
            {
                if (IsBlindFiring)
                {
                    if (IsInCover)
                        return IsInCoverFacingLeft ? "blindfire_low_l_aim_med" : "blindfire_low_r_aim_med";
                    return IsInCoverFacingLeft ? "blindfire_hi_l_aim_med" : "blindfire_hi_r_aim_med";
                }

                return IsInCoverFacingLeft ? "idle_l_corner" : "idle_r_corner";
            }

            return null;
        }

        internal string GetCoverIdleAnimDict()
        {
            if (!IsInCover) return "";
            var altitude = IsInLowCover ? "low" : "high";

            var hands = GetWeaponHandsHeld((uint)CurrentWeapon);
            if (IsBlindFiring)
            {
                if (hands == 1) return "cover@weapon@1h";
                if (hands == 2 || hands == 5) return "cover@weapon@2h";
            }

            if (hands == 1) return "cover@idles@1h@" + altitude + "@_a";
            if (hands == 2 || hands == 5) return "cover@idles@2h@" + altitude + "@_a";
            if (hands == 3 || hands == 4 || hands == 0) return "cover@idles@unarmed@" + altitude + "@_a";
            return "";
        }

        internal int GetWeaponHandsHeld(uint weapon)
        {
            switch (weapon)
            {
                case (uint)WeaponHash.Unarmed:
                    return 0;

                case (uint)WeaponHash.RPG:
                case (uint)WeaponHash.HomingLauncher:
                case (uint)WeaponHash.Firework:
                    return 5;

                case (uint)WeaponHash.Minigun:
                    return 5;

                case (uint)WeaponHash.GolfClub:
                case (uint)WeaponHash.PoolCue:
                case (uint)WeaponHash.Bat:
                    return 4;

                case (uint)WeaponHash.Knife:
                case (uint)WeaponHash.Nightstick:
                case (uint)WeaponHash.Hammer:
                case (uint)WeaponHash.Crowbar:
                case (uint)WeaponHash.Wrench:
                case (uint)WeaponHash.BattleAxe:
                case (uint)WeaponHash.Dagger:
                case (uint)WeaponHash.Hatchet:
                case (uint)WeaponHash.KnuckleDuster:
                case unchecked((uint)-581044007):
                case unchecked((uint)-102323637):
                case unchecked((uint)-538741184):
                    return 3;

                case unchecked((uint)-1357824103):
                case unchecked((uint)-1074790547):
                case 2132975508:
                case unchecked((uint)-2084633992):
                case unchecked((uint)-952879014):
                case 100416529:
                case (uint)WeaponHash.Gusenberg:
                case (uint)WeaponHash.MG:
                case (uint)WeaponHash.CombatMG:
                case (uint)WeaponHash.CombatPDW:
                case (uint)WeaponHash.AssaultSMG:
                case (uint)WeaponHash.SMG:
                case (uint)WeaponHash.HeavySniper:
                case (uint)WeaponHash.PumpShotgun:
                case (uint)WeaponHash.HeavyShotgun:
                case (uint)WeaponHash.Musket:
                case (uint)WeaponHash.AssaultShotgun:
                case (uint)WeaponHash.BullpupShotgun:
                case (uint)WeaponHash.SawnOffShotgun:
                case (uint)WeaponHash.SweeperShotgun:
                case (uint)WeaponHash.CompactRifle:
                    return 2;
            }

            return 1;
        }

        private string LoadAnim(string anim)
        {
            if (!Call<bool>(HAS_ANIM_DICT_LOADED, anim))
            {
                Call(REQUEST_ANIM_DICT, anim);
                return null;
            }

            return anim;
        }
    }
}