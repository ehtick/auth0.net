using System.Collections.Generic;

using Microsoft.IdentityModel.Tokens;

namespace Auth0.AuthenticationApi.Models.Ciba;

/// <summary>
/// Contains information required for request token using CIBA.
/// </summary>
public class ClientInitiatedBackchannelAuthorizationTokenRequest : IClientAuthentication
{
    /// <inheritdoc cref="IClientAuthentication.ClientId"/>
    public string ClientId { get; set; }

    /// <inheritdoc cref="IClientAuthentication.ClientSecret"/>
    public string ClientSecret { get; set; }
        
    /// <inheritdoc cref="IClientAuthentication.ClientAssertionSecurityKey"/>
    public SecurityKey ClientAssertionSecurityKey { get; set; }
        
    /// <inheritdoc cref="IClientAuthentication.ClientAssertionSecurityKeyAlgorithm"/>
    public string ClientAssertionSecurityKeyAlgorithm { get; set; }

    /// <summary>
    /// Unique identifier of the authorization request. This value will be returned from the call to /bc-authorize.
    /// </summary>
    public string AuthRequestId { get; set; }

    /// <summary>
    /// What <see cref="JwtSignatureAlgorithm"/> is used to verify the signature of Id Tokens.
    /// </summary>
    public JwtSignatureAlgorithm SigningAlgorithm { get; set; }

    /// <summary>
    /// Organization for Id Token verification.
    /// </summary>
    /// <remarks>
    /// - If you provide an Organization ID (a string with the prefix <c>org_</c>), it will be validated against the <c>org_id</c> claim of your user's ID Token. The validation is case-sensitive.
    /// - If you provide an Organization Name (a string <em>without</em> the prefix <c>org_</c>), it will be validated against the <c>org_name</c> claim of your user's ID Token. The validation is case-insensitive.
    /// </remarks>
    public string Organization { get; set; }

    /// <summary>
    /// Any additional properties to use.
    /// </summary>
    public IDictionary<string, string> AdditionalProperties { get; set; } = new Dictionary<string, string>();
}