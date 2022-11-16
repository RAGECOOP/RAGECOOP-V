using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace RageCoop.Core
{
    internal class AnimDic
    {
        public string[] Animations;
        public string DictionaryName;

        public static AnimDic[] Dump(string input, string output)
        {
            Console.WriteLine("Generating " + output);
            if (!File.Exists(input))
            {
                Console.WriteLine("Downloading");
                HttpHelper.DownloadFile(
                    "https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/animDictsCompact.json", input);
            }

            Console.WriteLine("Deserializing");
            var anims = JsonConvert.DeserializeObject<AnimDic[]>(File.ReadAllText(input));
            Console.WriteLine("Serializing");
            File.WriteAllText(output, JsonConvert.SerializeObject(anims, Formatting.Indented));
            return anims;
        }
    }

    internal class WeaponInfo
    {
        public string Audio;
        public float Damage;
        public string DamageType;
        public string FireType;
        public bool IsVehicleWeapon;
        public string Name;
        public float Speed;

        public WeaponInfo()
        {
        }

        public WeaponInfo(XmlNode node)
        {
            if (node.Attributes["type"].Value != "CWeaponInfo") throw new Exception("Not a CWeaponInfo node");
            foreach (XmlNode info in node.ChildNodes)
                switch (info.Name)
                {
                    case "Name":
                        Name = info.InnerText;
                        break;
                    case "Audio":
                        Audio = info.InnerText;
                        break;
                    case "FireType":
                        FireType = info.InnerText;
                        break;
                    case "DamageType":
                        DamageType = info.InnerText;
                        break;
                    case "Damage":
                        Damage = info.GetFloat();
                        break;
                    case "Speed":
                        Speed = info.GetFloat();
                        break;
                }

            IsVehicleWeapon = Name.StartsWith("VEHICLE_WEAPON");
        }
    }

    internal class VehicleInfo
    {
        public VehicleBone[] Bones;
        public uint Hash;
        public string Name;
        public string[] Weapons;
    }

    internal class VehicleBone
    {
        public uint BoneID;
        public uint BoneIndex;
        public string BoneName;
    }

    internal class WeaponBones
    {
        public VehicleBone[] Bones;
        public string Name;
    }

    internal class VehicleWeaponInfo
    {
        public uint Hash;
        public string Name;
        public Dictionary<uint, WeaponBones> Weapons = new Dictionary<uint, WeaponBones>();

        public static void Dump(string input, string output)
        {
            Console.WriteLine("Generating " + output);
            if (!File.Exists(input))
            {
                Console.WriteLine("Downloading");
                HttpHelper.DownloadFile(
                    "https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/vehicles.json", input);
            }

            Console.WriteLine("Deserializing");
            var infos = JsonConvert.DeserializeObject<VehicleInfo[]>(File.ReadAllText(input));
            Console.WriteLine("Serializing");
            File.WriteAllText(output,
                JsonConvert.SerializeObject(
                    infos.Select(FromVehicle).Where(x => x != null),
                    Formatting.Indented));
        }

        public static VehicleWeaponInfo FromVehicle(VehicleInfo info)
        {
            if (info.Weapons.Length == 0) return null;
            var result = new VehicleWeaponInfo { Hash = info.Hash, Name = info.Name };
            for (var i = 0; i < info.Weapons.Length; i++)
                result.Weapons.Add(CoreUtils.JoaatHash(info.Weapons[i])
                    , new WeaponBones
                    {
                        Name = info.Weapons[i],
                        Bones = info.Bones.Where(x =>
                            x.BoneName.StartsWith($"weapon_{i + 1}") && !x.BoneName.EndsWith("rot")).ToArray()
                    });
            return result;
        }
    }
}