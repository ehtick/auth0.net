using Auth0.ManagementApi;
using Auth0.ManagementApi.Core;
using global::System.Text.Json.Serialization;

namespace Auth0.ManagementApi.Users;

[Serializable]
public record CreateUserAuthenticationMethodRequestContent
{
    [JsonPropertyName("type")]
    public required CreatedUserAuthenticationMethodTypeEnum Type { get; set; }

    /// <summary>
    /// A human-readable label to identify the authentication method.
    /// </summary>
    [Optional]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Base32 encoded secret for TOTP generation.
    /// </summary>
    [Optional]
    [JsonPropertyName("totp_secret")]
    public string? TotpSecret { get; set; }

    /// <summary>
    /// Applies to phone authentication methods only. The destination phone number used to send verification codes via text and voice.
    /// </summary>
    [Optional]
    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Applies to email authentication methods only. The email address used to send verification messages.
    /// </summary>
    [Optional]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [Optional]
    [JsonPropertyName("preferred_authentication_method")]
    public PreferredAuthenticationMethodEnum? PreferredAuthenticationMethod { get; set; }

    /// <summary>
    /// Applies to webauthn/passkey authentication methods only. The id of the credential.
    /// </summary>
    [Optional]
    [JsonPropertyName("key_id")]
    public string? KeyId { get; set; }

    /// <summary>
    /// Applies to webauthn/passkey authentication methods only. The public key, which is encoded as base64.
    /// </summary>
    [Optional]
    [JsonPropertyName("public_key")]
    public string? PublicKey { get; set; }

    /// <summary>
    /// Applies to passkeys only. Authenticator Attestation Globally Unique Identifier
    /// </summary>
    [Optional]
    [JsonPropertyName("aaguid")]
    public string? Aaguid { get; set; }

    /// <summary>
    /// Applies to webauthn authentication methods only. The relying party identifier.
    /// </summary>
    [Optional]
    [JsonPropertyName("relying_party_identifier")]
    public string? RelyingPartyIdentifier { get; set; }

    [Optional]
    [JsonPropertyName("credential_device_type")]
    public CredentialDeviceTypeEnum? CredentialDeviceType { get; set; }

    /// <summary>
    /// Applies to passkeys only. Whether the credential was backed up.
    /// </summary>
    [Optional]
    [JsonPropertyName("credential_backed_up")]
    public bool? CredentialBackedUp { get; set; }

    /// <summary>
    /// Applies to passkeys only. The ID of the user identity linked with the authentication method.
    /// </summary>
    [Optional]
    [JsonPropertyName("identity_user_id")]
    public string? IdentityUserId { get; set; }

    /// <summary>
    /// Applies to passkeys only. The user-agent of the browser used to create the passkey.
    /// </summary>
    [Optional]
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Applies to passkeys only. The user handle of the user identity.
    /// </summary>
    [Optional]
    [JsonPropertyName("user_handle")]
    public string? UserHandle { get; set; }

    /// <summary>
    /// Applies to passkeys only. The transports used by clients to communicate with the authenticator.
    /// </summary>
    [Optional]
    [JsonPropertyName("transports")]
    public IEnumerable<string>? Transports { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonUtils.Serialize(this);
    }
}
