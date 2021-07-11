using System;
using System.IO;
using System.Xml.Serialization;

namespace CoopServer
{
    class Util
    {
        public static T Read<T>(string file) where T : new()
        {
            XmlSerializer ser = new(typeof(T));

            string path = AppContext.BaseDirectory + file;
            T settings;

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    settings = (T)ser.Deserialize(stream);
                }

                using (FileStream stream = new(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
                {
                    ser.Serialize(stream, settings);
                }
            }
            else
            {
                using FileStream stream = File.OpenWrite(path);
                ser.Serialize(stream, settings = new T());
            }

            return settings;
        }
    }
}
