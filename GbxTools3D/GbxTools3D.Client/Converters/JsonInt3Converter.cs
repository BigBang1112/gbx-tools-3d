﻿using GBX.NET;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace GbxTools3D.Client.Converters;

public class JsonInt3Converter : JsonConverter<Int3>
{
    public override Int3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(Int3));

        reader.Read();
        var x = reader.GetInt32();
        reader.Read();
        var y = reader.GetInt32();
        reader.Read();
        var z = reader.GetInt32();
        reader.Read();

        return new Int3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Int3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}
