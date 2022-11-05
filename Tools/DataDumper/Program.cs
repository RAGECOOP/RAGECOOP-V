using System.Text;
using System.Xml;
using Newtonsoft.Json;
using RageCoop.Core;
using Formatting = Newtonsoft.Json.Formatting;

namespace DataDumper;

internal class WeaponInfo
{
    public string Audio;
    public float Damage;
    public string DamageType;
    public string FireType;
    public bool IsVehicleWeapon;
    public string Name;
    public float Speed;

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

public static class Program
{
    public static float GetFloat(this XmlNode n)
    {
        return float.Parse(n.Attributes["value"].Value);
    }

    public static void Main()
    {
        Dictionary<uint, WeaponInfo> weapons = new();
        foreach (var f in Directory.GetFiles("meta", "*.meta")) Parse(f, weapons);
        Directory.CreateDirectory("Weapons");
        File.WriteAllText("Weapons\\Weapons.json", JsonConvert.SerializeObject(weapons, Formatting.Indented));
        DumpWeaponHash(weapons, true);
    }

    private static void Parse(string filename, Dictionary<uint, WeaponInfo> weap)
    {
        Console.WriteLine("Parsing " + filename);
        var doc = new XmlDocument();
        try
        {
            doc.Load(filename);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }

        var nodes = doc.ChildNodes.ToList();
        if (nodes.Any(x => x.Name == "CWeaponInfoBlob"))
        {
            var infosNode = doc.GetElementsByTagName("Item");
            foreach (XmlNode n in infosNode)
                if (n.Attributes["type"]?.Value == "CWeaponInfo")
                {
                    var info = new WeaponInfo(n);
                    if (!info.Name.StartsWith("VEHICLE_WEAPON") && !info.Name.StartsWith("WEAPON")) continue;
                    var hash = Hash(info.Name);
                    if (weap.ContainsKey(hash))
                        weap[hash] = info;
                    else
                        weap.Add(hash, info);
                }
        }
    }

    private static void DumpWeaponHash(Dictionary<uint, WeaponInfo> weapons, bool sort = false,
        string path = @"Weapons\WeaponHash.cs")
    {
        StringBuilder output = new();
        List<string> lines = new();
        var weps = weapons.Where(x => x.Value.Name.StartsWith("WEAPON"));
        var vehWeaps = weapons.Where(x => x.Value.Name.StartsWith("VEHICLE_WEAPON"));
        output.Append("public enum WeaponHash : uint\r\n{");
        foreach (var info in weps)
            lines.Add($"{CoreUtils.FormatToSharpStyle(info.Value.Name, 7)} = {info.Key.ToHex()}");
        if (sort) lines.Sort();
        foreach (var l in lines) output.Append($"\r\n\t{l},");
        output.AppendLine("\r\n}");
        output.AppendLine();
        output.Append("public enum VehicleWeaponHash : uint\r\n{\r\n\tInvalid = 0xFFFFFFFF,");
        lines = new List<string>();
        foreach (var info in vehWeaps)
            lines.Add($"{CoreUtils.FormatToSharpStyle(info.Value.Name)} = {info.Key.ToHex()}");
        if (sort) lines.Sort();
        foreach (var l in lines) output.Append($"\r\n\t{l},");
        output.Append("\r\n}");
        File.WriteAllText(path, output.ToString());
    }

    private static List<XmlNode> ToList(this XmlNodeList l)
    {
        var nodes = new List<XmlNode>();
        foreach (XmlNode n in l) nodes.Add(n);
        return nodes;
    }

    private static uint Hash(string key)
    {
        key = key.ToLower();
        var i = 0;
        uint hash = 0;
        while (i != key.Length)
        {
            hash += key[i++];
            hash += hash << 10;
            hash ^= hash >> 6;
        }

        hash += hash << 3;
        hash ^= hash >> 11;
        hash += hash << 15;
        return hash;
    }
}