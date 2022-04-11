using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;

using GTA;
using GTA.Math;
using GTA.Native;

namespace CoopClient
{
    /// <summary>
    /// 
    /// </summary>
    [XmlRoot(ElementName = "Map")]
    public class CoopMap
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlArray("Props")]
        [XmlArrayItem("Prop")]
        public List<CoopProp> Props { get; set; } = new List<CoopProp>();
    }

    /// <summary>
    /// 
    /// </summary>
    public struct CoopProp
    {
        /// <summary>
        /// 
        /// </summary>
        public Vector3 Position { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Vector3 Rotation { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Hash { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Dynamic { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Texture { get; set; }
    }

    internal static class MapLoader
    {
        // string = file name
        private static readonly Dictionary<string, CoopMap> _maps = new Dictionary<string, CoopMap>();
        private static readonly List<int> _createdObjects = new List<int>();

        public static void LoadAll()
        {
            string downloadFolder = $"scripts\\resources\\{Main.MainSettings.LastServerAddress.Replace(":", ".")}";

            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            string[] files = Directory.GetFiles(downloadFolder, "*.xml");
            lock (_maps)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string filePath = files[i];

                    XmlSerializer serializer = new XmlSerializer(typeof(CoopMap));
                    CoopMap map;

                    using (var stream = new FileStream(filePath, FileMode.Open))
                    {
                        map = (CoopMap)serializer.Deserialize(stream);
                    }

                    GTA.UI.Notification.Show($"{map.Props.Count()}");

                    string fileName = Path.GetFileName(filePath);
                    _maps.Add(fileName, map);

                    GTA.UI.Notification.Show($"test: {_maps["ATV.xml"].Props.Count()}");
                }

                //GTA.UI.Notification.Show($"{_maps["ATV.xml"].Objects[0].Position.X}");
            }
        }

        public static void LoadMap(string name)
        {
            lock (_maps) lock (_createdObjects)
            {
                if (!_maps.ContainsKey(name) || _createdObjects.Count != 0)
                {
                    GTA.UI.Notification.Show($"The map with the name \"{name}\" couldn't be loaded!");
                    Logger.Write($"The map with the name \"{name}\" couldn't be loaded!", Logger.LogLevel.Server);
                    return;
                }
        
                CoopMap map = _maps[name];
        
                for (int i = 0; i < map.Props.Count(); i++)
                {
                    CoopProp prop = map.Props[i];
        
                    Model model = prop.Hash.ModelRequest();
                    if (model == null)
                    {
                        Logger.Write($"Model for object \"{model.Hash}\" couldn't be loaded!", Logger.LogLevel.Server);
                        continue;
                    }
        
                    int handle = Function.Call<int>(Hash.CREATE_OBJECT, model.Hash, prop.Position.X, prop.Position.Y, prop.Position.Z, 1, 1, prop.Dynamic);
                    if (handle == 0)
                    {
                        Logger.Write($"Object \"{model.Hash}\" couldn't be created!", Logger.LogLevel.Server);
                        continue;
                    }
                    model.MarkAsNoLongerNeeded();

                    _createdObjects.Add(handle);
        
                    if (prop.Texture > 0 && prop.Texture < 16)
                    {
                        Function.Call(Hash._SET_OBJECT_TEXTURE_VARIATION, handle, prop.Texture);
                    }

                    Logger.Write($"Object [{model.Hash}] created at {prop.Position.X}, {prop.Position.Y}, {prop.Position.Z}", Logger.LogLevel.Server);
                }
            }
        }

        public static bool AnyMapLoaded()
        {
            return _createdObjects.Any();
        }

        public static void DeleteMap()
        {
            lock (_createdObjects)
            {
                foreach (int handle in _createdObjects)
                {
                    unsafe
                    {
                        int tmpHandle = handle;
                        Function.Call(Hash.DELETE_OBJECT, &tmpHandle);
                    }
                }

                _createdObjects.Clear();
            }
        }

        public static void DeleteAll()
        {
            DeleteMap();
            lock (_maps)
            {
                _maps.Clear();
            }
        }
    }
}
