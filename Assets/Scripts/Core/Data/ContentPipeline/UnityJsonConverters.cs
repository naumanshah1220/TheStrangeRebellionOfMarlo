using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Newtonsoft.Json converter for UnityEngine.Color.
/// Reads/writes {"r":1,"g":0.5,"b":0,"a":1} format.
/// </summary>
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return Color.white;

        var obj = JObject.Load(reader);
        return new Color(
            obj.Value<float>("r"),
            obj.Value<float>("g"),
            obj.Value<float>("b"),
            obj.ContainsKey("a") ? obj.Value<float>("a") : 1f
        );
    }

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("r"); writer.WriteValue(value.r);
        writer.WritePropertyName("g"); writer.WriteValue(value.g);
        writer.WritePropertyName("b"); writer.WriteValue(value.b);
        writer.WritePropertyName("a"); writer.WriteValue(value.a);
        writer.WriteEndObject();
    }
}
