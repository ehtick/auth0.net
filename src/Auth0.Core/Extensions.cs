using System;
using System.Collections.Generic;
using System.Linq;

using Auth0.Core.Exceptions;

namespace Auth0.Core;

public static class Extensions
{
    /// <summary>
    /// Extracts the <see cref="ClientQuotaLimit"/> from the response headers.
    /// </summary>
    /// <param name="headers">The source response headers</param>
    /// <returns><see cref="ClientQuotaLimit"/></returns>
    public static ClientQuotaLimit? GetClientQuotaLimit(this IDictionary<string, IEnumerable<string>> headers)
    {
        return ParseClientLimit(GetRawHeaders(headers, "Auth0-Client-Quota-Limit"));
    }

    /// <summary>
    /// Extracts the <see cref="OrganizationQuotaLimit"/> from the response headers
    /// </summary>
    /// <param name="headers">The source response headers</param>
    /// <returns><see cref="OrganizationQuotaLimit"/></returns>
    public static OrganizationQuotaLimit? GetOrganizationQuotaLimit(
        this IDictionary<string, IEnumerable<string>> headers)
    {
        return ParseOrganizationLimit(GetRawHeaders(headers, "Auth0-Organization-Quota-Limit"));
    }

    internal static string? GetRawHeaders(IDictionary<string, IEnumerable<string>> headers, string headerName)
    {
        if (headers == null)
        {
            return null;
        }
        return !headers.TryGetValue(headerName, out var values) ? null : values.FirstOrDefault();
    }
        
    internal static ClientQuotaLimit? ParseClientLimit(string? headerValue)
    {
        if (string.IsNullOrEmpty(headerValue))
        {
            return null;
        }
        var buckets = headerValue!.Split(',');
        var quotaClientLimit = new ClientQuotaLimit();
        foreach (var eachBucket in buckets)
        {
            var quotaLimit = ParseQuotaLimit(eachBucket, out var bucket);
            if (bucket == "per_hour")
            {
                quotaClientLimit.PerHour = quotaLimit;
            }
            else
            {
                quotaClientLimit.PerDay = quotaLimit;    
            }
        }

        return quotaClientLimit;
    }

    internal static OrganizationQuotaLimit? ParseOrganizationLimit(string? headerValue)
    {
        if (string.IsNullOrEmpty(headerValue))
        {
            return null;
        }
            
        var buckets = headerValue!.Split(',');
        var quotaOrganizationLimit = new OrganizationQuotaLimit();
        foreach (var eachBucket in buckets)
        {
            var quotaLimit = ParseQuotaLimit(eachBucket, out var bucket);
            if (bucket == "per_hour")
            {
                quotaOrganizationLimit.PerHour = quotaLimit;
                continue;
            }

            quotaOrganizationLimit.PerDay = quotaLimit;
        }

        return quotaOrganizationLimit;
    }

    internal static QuotaLimit? ParseQuotaLimit(string headerValue, out string? bucket)
    {
        bucket = null;

        if (string.IsNullOrEmpty(headerValue))
            return null;

        try
        {
            var kvp = new Dictionary<string, string>();
            foreach (var segment in headerValue.Split(';'))
            {
                var parts = segment.Split(['='], 2);
                if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                    return null;

                var key = parts[0];
                var value = parts[1];

                // Duplicate keys indicate a malformed header
                if (kvp.ContainsKey(key))
                    return null;

                kvp[key] = value;
            }

            if (!kvp.TryGetValue("b", out var bucketValue)
                || !kvp.TryGetValue("q", out var quota)
                || !kvp.TryGetValue("r", out var remaining)
                || !kvp.TryGetValue("t", out var resetAfter))
            {
                return null;
            }

            bucket = bucketValue;
            return new QuotaLimit
            {
                Quota = int.Parse(quota),
                Remaining = int.Parse(remaining),
                ResetAfter = int.Parse(resetAfter)
            };
        }
        catch (FormatException)
        {
            // Unable to parse integers from the header values, indicating a malformed header
            return null;
        }
        catch (OverflowException)
        {
            // Unable to parse integers from the header values due to overflow, indicating a malformed header
            return null;
        }
    }
}