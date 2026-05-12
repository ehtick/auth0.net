using Auth0.ManagementApi.Core;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

namespace Auth0.ManagementApi;

[JsonConverter(typeof(TooManyRequestsSchemaError.TooManyRequestsSchemaErrorSerializer))]
[Serializable]
public readonly record struct TooManyRequestsSchemaError : IStringEnum
{
    public static readonly TooManyRequestsSchemaError TooManyRequests = new(Values.TooManyRequests);

    public TooManyRequestsSchemaError(string value)
    {
        Value = value;
    }

    /// <summary>
    /// The string value of the enum.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Create a string enum with the given value.
    /// </summary>
    public static TooManyRequestsSchemaError FromCustom(string value)
    {
        return new TooManyRequestsSchemaError(value);
    }

    public bool Equals(string? other)
    {
        return Value.Equals(other);
    }

    /// <summary>
    /// Returns the string value of the enum.
    /// </summary>
    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(TooManyRequestsSchemaError value1, string value2) =>
        value1.Value.Equals(value2);

    public static bool operator !=(TooManyRequestsSchemaError value1, string value2) =>
        !value1.Value.Equals(value2);

    public static explicit operator string(TooManyRequestsSchemaError value) => value.Value;

    public static explicit operator TooManyRequestsSchemaError(string value) => new(value);

    internal class TooManyRequestsSchemaErrorSerializer : JsonConverter<TooManyRequestsSchemaError>
    {
        public override TooManyRequestsSchemaError Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var stringValue =
                reader.GetString()
                ?? throw new global::System.Exception(
                    "The JSON value could not be read as a string."
                );
            return new TooManyRequestsSchemaError(stringValue);
        }

        public override void Write(
            Utf8JsonWriter writer,
            TooManyRequestsSchemaError value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.Value);
        }

        public override TooManyRequestsSchemaError ReadAsPropertyName(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var stringValue =
                reader.GetString()
                ?? throw new global::System.Exception(
                    "The JSON property name could not be read as a string."
                );
            return new TooManyRequestsSchemaError(stringValue);
        }

        public override void WriteAsPropertyName(
            Utf8JsonWriter writer,
            TooManyRequestsSchemaError value,
            JsonSerializerOptions options
        )
        {
            writer.WritePropertyName(value.Value);
        }
    }

    /// <summary>
    /// Constant strings for enum values
    /// </summary>
    [Serializable]
    public static class Values
    {
        public const string TooManyRequests = "Too Many Requests";
    }
}
