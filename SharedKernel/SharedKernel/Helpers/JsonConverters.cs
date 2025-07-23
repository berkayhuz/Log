namespace SharedKernel.Helpers;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using SharedKernel.Common.Results.Objects;

public class ErrorLevelConverter : JsonConverter<ErrorLevel>
{
    public override ErrorLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();

        if (Enum.TryParse<ErrorLevel>(stringValue, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new JsonException($"Invalid value '{stringValue}' for ErrorLevel.");
    }

    public override void Write(Utf8JsonWriter writer, ErrorLevel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
