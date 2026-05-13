using Auth0.ManagementApi.Core;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

namespace Auth0.ManagementApi;

[JsonConverter(typeof(UnauthorizedSchemaError.UnauthorizedSchemaErrorSerializer))]
[Serializable]
public readonly record struct UnauthorizedSchemaError : IStringEnum
{
    public static readonly UnauthorizedSchemaError Unauthorized = new(Values.Unauthorized);

    public UnauthorizedSchemaError(string value)
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
    public static UnauthorizedSchemaError FromCustom(string value)
    {
        return new UnauthorizedSchemaError(value);
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

    public static bool operator ==(UnauthorizedSchemaError value1, string value2) =>
        value1.Value.Equals(value2);

    public static bool operator !=(UnauthorizedSchemaError value1, string value2) =>
        !value1.Value.Equals(value2);

    public static explicit operator string(UnauthorizedSchemaError value) => value.Value;

    public static explicit operator UnauthorizedSchemaError(string value) => new(value);

    internal class UnauthorizedSchemaErrorSerializer : JsonConverter<UnauthorizedSchemaError>
    {
        public override UnauthorizedSchemaError Read(
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
            return new UnauthorizedSchemaError(stringValue);
        }

        public override void Write(
            Utf8JsonWriter writer,
            UnauthorizedSchemaError value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.Value);
        }

        public override UnauthorizedSchemaError ReadAsPropertyName(
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
            return new UnauthorizedSchemaError(stringValue);
        }

        public override void WriteAsPropertyName(
            Utf8JsonWriter writer,
            UnauthorizedSchemaError value,
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
        public const string Unauthorized = "Unauthorized";
    }
}
