namespace SharedKernel.Common.Results.Json;
using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class ResultJsonConverter<T> : JsonConverter<Result<T>>
{
    public override Result<T>? ReadJson(JsonReader reader, Type objectType, Result<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        var isSuccess = jObject["IsSuccess"]?.Value<bool>() ?? false;
        var errors = jObject["Errors"]?.ToObject<List<string>>(serializer) ?? new();

        if (isSuccess)
        {
            if (!jObject.TryGetValue("Value", out var valueToken))
                throw new JsonException("Expected 'Value' property for a successful result.");

            var value = valueToken.ToObject<T>(serializer);

            if (value is null)
                throw new JsonException($"'Value' property could not be deserialized to type '{typeof(T)}'.");

            return Result<T>.Success(value);
        }


        return Result<T>.Failure(errors);
    }

    public override void WriteJson(JsonWriter writer, Result<T> value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("IsSuccess");
        writer.WriteValue(value.IsSuccess);

        writer.WritePropertyName("Value");
        serializer.Serialize(writer, value.Value);

        writer.WritePropertyName("Errors");
        serializer.Serialize(writer, value.Errors);

        if (value is Result baseResult)
        {
            writer.WritePropertyName("ErrorCodes");
            serializer.Serialize(writer, baseResult.ErrorCodes);

            writer.WritePropertyName("Metadata");
            serializer.Serialize(writer, baseResult.Metadata);
        }

        writer.WriteEndObject();
    }
    public override bool CanRead => true;
    public override bool CanWrite => true;
}
