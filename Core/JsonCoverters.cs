using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RageCoop.Core
{
    class IPAddressConverter : JsonConverter<IPAddress>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPAddress);
        }

        public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return IPAddress.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString());
        }
    }

    class IPEndPointConverter : JsonConverter<IPEndPoint>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPEndPoint);
        }
        public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            var jo = JsonNode.Parse(ref reader);
            return new IPEndPoint(IPAddress.Parse(jo["Address"].ToString()), int.Parse(jo["Port"].ToString()));
        }

        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
        {
            new JsonObject()
            {
                { "Address", value.Address?.ToString() },
                { "Port", value.Port }
            }.WriteTo(writer);
        }
    }

}
