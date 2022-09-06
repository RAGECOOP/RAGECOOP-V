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
                Function.Call(Hash.PLAY_FACIAL_ANIM, MainPed.Handle, "mic_chatter", "mp_facial");
                return;
            }

            switch (MainPed.Gender)
            {
                case Gender.Male:
                    Function.Call(Hash.PLAY_FACIAL_ANIM, MainPed.Handle, "mood_normal_1", "facials@gen_male@variations@normal");
                    break;
                case Gender.Female:
                    Function.Call(Hash.PLAY_FACIAL_ANIM, MainPed.Handle, "mood_normal_1", "facials@gen_female@variations@normal");
                    break;
                default:
                    Function.Call(Hash.PLAY_FACIAL_ANIM, MainPed.Handle, "mood_normal_1", "facials@mime@variations@normal");
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
                if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed, animDict, ourAnim, 3))
                {
                    MainPed.Task.ClearAll();
                    Function.Call(Hash.TASK_PLAY_ANIM, MainPed, LoadAnim(animDict), ourAnim, 8f, 10f, -1, flag, -8f, 1, 1, 1);
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

            var hands = GetWeaponHandsHeld(CurrentWeaponHash);
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
                case unchecked((uint)WeaponHash.Unarmed):
                    return 0;

                case unchecked((uint)WeaponHash.RPG):
                case unchecked((uint)WeaponHash.HomingLauncher):
                case unchecked((uint)WeaponHash.Firework):
                    return 5;

                case unchecked((uint)WeaponHash.Minigun):
                    return 5;

                case unchecked((uint)WeaponHash.GolfClub):
                case unchecked((uint)WeaponHash.PoolCue):
                case unchecked((uint)WeaponHash.Bat):
                    return 4;

                case unchecked((uint)WeaponHash.Knife):
                case unchecked((uint)WeaponHash.Nightstick):
                case unchecked((uint)WeaponHash.Hammer):
                case unchecked((uint)WeaponHash.Crowbar):
                case unchecked((uint)WeaponHash.Wrench):
                case unchecked((uint)WeaponHash.BattleAxe):
                case unchecked((uint)WeaponHash.Dagger):
                case unchecked((uint)WeaponHash.Hatchet):
                case unchecked((uint)WeaponHash.KnuckleDuster):
                case unchecked((uint)-581044007):
                case unchecked((uint)-102323637):
                case unchecked((uint)-538741184):
                    return 3;

                case unchecked((uint)-1357824103):
                case unchecked((uint)-1074790547):
                case unchecked(2132975508):
                case unchecked((uint)-2084633992):
                case unchecked((uint)-952879014):
                case unchecked(100416529):
                case unchecked((uint)WeaponHash.Gusenberg):
                case unchecked((uint)WeaponHash.MG):
                case unchecked((uint)WeaponHash.CombatMG):
                case unchecked((uint)WeaponHash.CombatPDW):
                case unchecked((uint)WeaponHash.AssaultSMG):
                case unchecked((uint)WeaponHash.SMG):
                case unchecked((uint)WeaponHash.HeavySniper):
                case unchecked((uint)WeaponHash.PumpShotgun):
                case unchecked((uint)WeaponHash.HeavyShotgun):
                case unchecked((uint)WeaponHash.Musket):
                case unchecked((uint)WeaponHash.AssaultShotgun):
                case unchecked((uint)WeaponHash.BullpupShotgun):
                case unchecked((uint)WeaponHash.SawnOffShotgun):
                case unchecked((uint)WeaponHash.SweeperShotgun):
                case unchecked((uint)WeaponHash.CompactRifle):
                    return 2;
            }

            return 1;
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
    }
}
