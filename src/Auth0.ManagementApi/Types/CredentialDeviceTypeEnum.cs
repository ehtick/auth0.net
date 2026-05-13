using Auth0.ManagementApi.Core;
using global::System.Text.Json;
using global::System.Text.Json.Serialization;

namespace Auth0.ManagementApi;

[JsonConverter(typeof(CredentialDeviceTypeEnum.CredentialDeviceTypeEnumSerializer))]
[Serializable]
public readonly record struct CredentialDeviceTypeEnum : IStringEnum
{
    public static readonly CredentialDeviceTypeEnum SingleDevice = new(Values.SingleDevice);

    public static readonly CredentialDeviceTypeEnum MultiDevice = new(Values.MultiDevice);

    public CredentialDeviceTypeEnum(string value)
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
    public static CredentialDeviceTypeEnum FromCustom(string value)
    {
        return new CredentialDeviceTypeEnum(value);
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

    public static bool operator ==(CredentialDeviceTypeEnum value1, string value2) =>
        value1.Value.Equals(value2);

    public static bool operator !=(CredentialDeviceTypeEnum value1, string value2) =>
        !value1.Value.Equals(value2);

    public static explicit operator string(CredentialDeviceTypeEnum value) => value.Value;

    public static explicit operator CredentialDeviceTypeEnum(string value) => new(value);

    internal class CredentialDeviceTypeEnumSerializer : JsonConverter<CredentialDeviceTypeEnum>
    {
        public override CredentialDeviceTypeEnum Read(
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
            return new CredentialDeviceTypeEnum(stringValue);
        }

        public override void Write(
            Utf8JsonWriter writer,
            CredentialDeviceTypeEnum value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.Value);
        }

        public override CredentialDeviceTypeEnum ReadAsPropertyName(
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
            return new CredentialDeviceTypeEnum(stringValue);
        }

        public override void WriteAsPropertyName(
            Utf8JsonWriter writer,
            CredentialDeviceTypeEnum value,
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
        public const string SingleDevice = "single_device";

        public const string MultiDevice = "multi_device";
    }
}
