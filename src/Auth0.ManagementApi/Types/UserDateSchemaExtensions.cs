using System.Globalization;

namespace Auth0.ManagementApi;

/// <summary>
/// Extension methods for <see cref="UserDateSchema"/>.
/// </summary>
public static class UserDateSchemaExtensions
{
    /// <summary>
    /// Parses the underlying string value into a <see cref="DateTime"/> using invariant culture and round-trip kind.
    /// Returns <c>null</c> if the schema is null or does not contain a valid date-time string.
    /// </summary>
    public static DateTime? ToDateTime(this UserDateSchema? schema)
    {
        if (schema is null || !schema.IsString())
            return null;

        return DateTime.TryParse(
            schema.AsString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var result
        )
            ? result
            : null;
    }

    /// <summary>
    /// Parses the underlying string value into a <see cref="DateTime"/>, then converts it to the provided time zone.
    /// Returns <c>null</c> if the schema is null or does not contain a valid date-time string.
    /// </summary>
    /// <remarks>
    /// For timestamps without an explicit offset, parsing uses the local machine time zone.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="targetTimeZone"/> is null.</exception>
    public static DateTime? ToDateTime(this UserDateSchema? schema, TimeZoneInfo targetTimeZone)
    {
        if (targetTimeZone is null)
            throw new ArgumentNullException(nameof(targetTimeZone));

        if (schema is null || !schema.IsString())
            return null;

        if (
            !DateTimeOffset.TryParse(
                schema.AsString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDateTimeOffset
            )
        )
        {
            return null;
        }

        var convertedDateTimeOffset = TimeZoneInfo.ConvertTime(parsedDateTimeOffset, targetTimeZone);

        return targetTimeZone.Equals(TimeZoneInfo.Utc)
            ? convertedDateTimeOffset.UtcDateTime
            : DateTime.SpecifyKind(convertedDateTimeOffset.DateTime, DateTimeKind.Unspecified);
    }
}
