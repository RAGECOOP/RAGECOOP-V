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
            T data;

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    data = (T)ser.Deserialize(stream);
                }

                using (FileStream stream = new(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
                {
                    ser.Serialize(stream, data);
                }
            }
            else
            {
                using FileStream stream = File.OpenWrite(path);
                ser.Serialize(stream, data = new T());
            }

            return data;
        }
    }
}
