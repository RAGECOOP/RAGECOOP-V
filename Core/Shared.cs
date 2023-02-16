global using static RageCoop.Core.Shared;
using Newtonsoft.Json;
using System;

namespace RageCoop.Core
{
    internal class Shared
    {
        public static readonly JsonSerializerSettings JsonSettings = new();
        static Shared()
        {
            JsonSettings.Converters.Add(new IPAddressConverter());
            JsonSettings.Converters.Add(new IPEndPointConverter());
            JsonSettings.Formatting = Formatting.Indented;
        }

        public static object JsonDeserialize(string text, Type type)
        {
            return JsonConvert.DeserializeObject(text, type, JsonSettings);
        }

        public static T JsonDeserialize<T>(string text) => (T)JsonDeserialize(text, typeof(T));

        public static string JsonSerialize(object obj) => JsonConvert.SerializeObject(obj, JsonSettings);

        /// <summary>
        /// Shortcut to <see cref="BufferReader.ThreadLocal"/>
        /// </summary>
        /// <returns></returns>
        public static unsafe BufferReader GetReader(byte* data = null, int cbData = 0)
        {
            var reader = BufferReader.ThreadLocal.Value;
            reader.Initialise(data, cbData);
            return reader;
        }


        /// <summary>
        /// Shortcut to <see cref="BufferWriter.ThreadLocal"/>
        /// </summary>
        /// <returns></returns>
        public static BufferWriter GetWriter(bool reset = true)
        {
            var writer = BufferWriter.ThreadLocal.Value;
            if (reset)
            {
                writer.Reset();
            }
            return writer;
        }
    }
}
