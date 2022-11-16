using System.Text;
using System.Xml;
using Newtonsoft.Json;
using RageCoop.Core;
using Formatting = Newtonsoft.Json.Formatting;

namespace DataDumper;

[Flags]
public enum GenFlags
{
    WeaponInfo = 1,
    WeaponHash = 2,
    VehicleWeaponInfo = 4,
    Animations = 8,
    All = ~0
}

public static class Program
{
    public static GenFlags ToGenerate = GenFlags.All;

    public static void Main(string[] args)
    {
        if (args.Length > 0 && Enum.TryParse<GenFlags>(args[0], true, out var flags)) ToGenerate = flags;
        Directory.CreateDirectory("out");

        #region META

        // Dumps from the game's xml documents, needs to have all *.meta file extracted to "meta" directory. OpenIV is recommended

        if (ToGenerate.HasFlag(GenFlags.WeaponInfo))
        {
            Dictionary<uint, WeaponInfo> weapons = new();
            foreach (var f in Directory.GetFiles("meta", "*.meta")) Parse(f, weapons);
            File.WriteAllText("out/Weapons.json", JsonConvert.SerializeObject(weapons, Formatting.Indented));
            if (ToGenerate.HasFlag(GenFlags.WeaponHash)) DumpWeaponHash(weapons, true);
        }

        #endregion


        #region EXTERNAL

        // External data from DurtyFree's data dumps: https://github.com/DurtyFree/gta-v-data-dumps

        Directory.CreateDirectory("ext");
        if (ToGenerate.HasFlag(GenFlags.VehicleWeaponInfo))
            VehicleWeaponInfo.Dump("ext/vehicles.json", "out/VehicleWeapons.json");

        if (ToGenerate.HasFlag(GenFlags.Animations)) AnimDic.Dump("ext/animDictsCompact.json", "out/Animations.json");

        #endregion
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
                    var hash = CoreUtils.JoaatHash(info.Name);
                    if (weap.ContainsKey(hash))
                        weap[hash] = info;
                    else
                        weap.Add(hash, info);
                }
        }
    }

    private static void DumpWeaponHash(Dictionary<uint, WeaponInfo> weapons, bool sort = false,
        string path = @"out/WeaponHash.cs")
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
            lines.Add($"{CoreUtils.FormatToSharpStyle(info.Value.Name, 14)} = {info.Key.ToHex()}");
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
}