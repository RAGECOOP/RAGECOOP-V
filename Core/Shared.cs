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
    }
}
