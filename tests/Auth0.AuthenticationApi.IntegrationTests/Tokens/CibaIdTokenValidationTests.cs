using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Auth0.AuthenticationApi.Models.Ciba;
using Auth0.AuthenticationApi.Tokens;
using Auth0.Tests.Shared;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;

namespace Auth0.AuthenticationApi.IntegrationTests.Tokens;

public class CibaIdTokenValidationTests
{
    private const string Issuer = "https://tokens-test.auth0.com/";
    private const string ClientId = "tokens-test-123";
    private const string Subject = "auth0|test-user";

    // RSA key pair used to sign RS256 test tokens — generated once per test class.
    private static readonly RsaSecurityKey RsaKey = new(new RSACryptoServiceProvider(2048));
    private static readonly JwtTokenFactory RsaTokenFactory = new(RsaKey, SecurityAlgorithms.RsaSha256);

    /// <summary>
    /// Generates an HS256 ID token signed with the supplied client secret.
    /// </summary>
    private static string GenerateHs256Token(string clientSecret, IList<Claim> extraClaims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(clientSecret));
        var factory = new JwtTokenFactory(key, SecurityAlgorithms.HmacSha256);
        return factory.GenerateToken(Issuer, ClientId, Subject, extraClaims);
    }

    /// <summary>
    /// Generates an HS256 ID token that is already expired.
    /// </summary>
    private static string GenerateExpiredHs256Token(string clientSecret)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(clientSecret));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenHandler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, Subject) }),
            NotBefore = DateTime.UtcNow.AddHours(-3),
            IssuedAt = DateTime.UtcNow.AddHours(-3),
            Expires = DateTime.UtcNow.AddHours(-1),
            Issuer = Issuer,
            Audience = ClientId,
            SigningCredentials = signingCredentials
        };
        return tokenHandler.WriteToken(tokenHandler.CreateToken(descriptor));
    }

    /// <summary>
    /// Generates an RS256 ID token signed with the shared RSA key.
    /// </summary>
    private static string GenerateRs256Token(IList<Claim> extraClaims = null)
        => RsaTokenFactory.GenerateToken(Issuer, ClientId, Subject, extraClaims);

    /// <summary>
    /// Builds a mock HttpMessageHandler that returns a CIBA token endpoint response
    /// containing the given idToken (null omits the field).
    /// </summary>
    private static Mock<HttpMessageHandler> BuildMockHandlerReturning(string idToken)
    {
        var responseBody = idToken != null
            ? new { access_token = "mock-access-token", token_type = "Bearer", expires_in = 86400, id_token = idToken }
            : (object)new { access_token = "mock-access-token", token_type = "Bearer", expires_in = 86400 };

        var json = JsonConvert.SerializeObject(responseBody);

        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        return mockHandler;
    }

    /// <summary>
    /// Builds a mock handler that handles the CIBA token POST plus the OIDC discovery
    /// and JWKS GET requests required for RS256 signature validation.
    /// </summary>
    private static Mock<HttpMessageHandler> BuildMockHandlerWithDiscovery(string idToken)
    {
        var tokenJson = JsonConvert.SerializeObject(new
        {
            access_token = "mock-access-token",
            token_type = "Bearer",
            expires_in = 86400,
            id_token = idToken
        });

        var rsaParams = RsaKey.Rsa.ExportParameters(false);
        var jwksJson = JsonConvert.SerializeObject(new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    n = Base64UrlEncoder.Encode(rsaParams.Modulus),
                    e = Base64UrlEncoder.Encode(rsaParams.Exponent)
                }
            }
        });

        var discoveryJson = JsonConvert.SerializeObject(new
        {
            issuer = Issuer,
            jwks_uri = $"{Issuer}.well-known/jwks.json"
        });

        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenJson, Encoding.UTF8, "application/json")
            });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri.AbsolutePath.Contains("openid-configuration")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(discoveryJson, Encoding.UTF8, "application/json")
            });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri.AbsolutePath.Contains("jwks")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jwksJson, Encoding.UTF8, "application/json")
            });

        return mockHandler;
    }

    private static TestAuthenticationApiClient BuildClient(HttpMessageHandler handler)
    {
        var domain = Issuer.TrimEnd('/').Replace("https://", "");
        return new TestAuthenticationApiClient(domain, new TestHttpClientAuthenticationConnection(handler));
    }
    
    private const string DummyClientSecret = "dummy-client-secret-for-testing";

    private static ClientInitiatedBackchannelAuthorizationTokenRequest BuildRequest(
        string clientSecret = DummyClientSecret,
        JwtSignatureAlgorithm algorithm = JwtSignatureAlgorithm.RS256,
        string organization = null)
    {
        return new ClientInitiatedBackchannelAuthorizationTokenRequest
        {
            ClientId = ClientId,
            ClientSecret = clientSecret,
            AuthRequestId = "test-auth-req-id",
            SigningAlgorithm = algorithm,
            Organization = organization
        };
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Succeeds_WhenNoIdTokenInResponse()
    {
        using var client = BuildClient(BuildMockHandlerReturning(null).Object);

        var result = await client.GetTokenAsync(BuildRequest());

        Assert.NotNull(result);
        Assert.Equal("mock-access-token", result.AccessToken);
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Succeeds_WithValidHs256IdToken()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        var idToken = GenerateHs256Token(clientSecret);

        using var client = BuildClient(BuildMockHandlerReturning(idToken).Object);

        var result = await client.GetTokenAsync(BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Succeeds_WithValidRs256IdToken()
    {
        var idToken = GenerateRs256Token();

        using var client = BuildClient(BuildMockHandlerWithDiscovery(idToken).Object);

        var result = await client.GetTokenAsync(BuildRequest(algorithm: JwtSignatureAlgorithm.RS256));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Throws_WhenIdTokenSignatureIsForged()
    {
        // An attacker intercepts the token response (via proxy / MITM) and replaces
        // the id_token with one signed using a different secret.
        var realSecret = Guid.NewGuid().ToString("N");
        var attackerSecret = Guid.NewGuid().ToString("N");
        var forgedToken = GenerateHs256Token(attackerSecret); // signed with wrong key

        using var client = BuildClient(BuildMockHandlerReturning(forgedToken).Object);

        await Assert.ThrowsAsync<IdTokenValidationException>(() =>
            client.GetTokenAsync(BuildRequest(realSecret, JwtSignatureAlgorithm.HS256)));
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Throws_WhenIdTokenIsExpired()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        var expiredToken = GenerateExpiredHs256Token(clientSecret);

        using var client = BuildClient(BuildMockHandlerReturning(expiredToken).Object);

        await Assert.ThrowsAsync<IdTokenValidationException>(() =>
            client.GetTokenAsync(BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256)));
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Throws_WhenIdTokenHasWrongAudience()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        // Token is issued for a different audience.
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(clientSecret));
        var factory = new JwtTokenFactory(key, SecurityAlgorithms.HmacSha256);
        var wrongAudienceToken = factory.GenerateToken(Issuer, "wrong-audience", Subject);

        using var client = BuildClient(BuildMockHandlerReturning(wrongAudienceToken).Object);

        await Assert.ThrowsAsync<IdTokenValidationException>(() =>
            client.GetTokenAsync(BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256)));
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Throws_WhenIdTokenHasWrongIssuer()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        // Token is issued by a different issuer.
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(clientSecret));
        var factory = new JwtTokenFactory(key, SecurityAlgorithms.HmacSha256);
        var wrongIssuerToken = factory.GenerateToken("https://evil.example.com/", ClientId, Subject);

        using var client = BuildClient(BuildMockHandlerReturning(wrongIssuerToken).Object);

        await Assert.ThrowsAsync<IdTokenValidationException>(() =>
            client.GetTokenAsync(BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256)));
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Succeeds_WhenOrgIdClaimMatchesRequest()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        var orgId = "org_abc123";
        var idToken = GenerateHs256Token(clientSecret, new List<Claim> { new("org_id", orgId) });

        using var client = BuildClient(BuildMockHandlerReturning(idToken).Object);

        var result = await client.GetTokenAsync(
            BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256, organization: orgId));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Throws_WhenOrgIdClaimMismatch()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        var idToken = GenerateHs256Token(clientSecret, new List<Claim> { new("org_id", "org_realorg") });

        using var client = BuildClient(BuildMockHandlerReturning(idToken).Object);

        await Assert.ThrowsAsync<IdTokenValidationException>(() =>
            client.GetTokenAsync(
                BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256, organization: "org_differentorg")));
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Succeeds_WhenOrgNameClaimMatchesRequest()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        var idToken = GenerateHs256Token(clientSecret, new List<Claim> { new("org_name", "mycompany") });

        using var client = BuildClient(BuildMockHandlerReturning(idToken).Object);

        var result = await client.GetTokenAsync(
            BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256, organization: "mycompany"));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Succeeds_WhenOrgNameMatchIsCaseInsensitive()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        var idToken = GenerateHs256Token(clientSecret, new List<Claim> { new("org_name", "mycompany") });

        using var client = BuildClient(BuildMockHandlerReturning(idToken).Object);

        var result = await client.GetTokenAsync(
            BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256, organization: "MyCompany"));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTokenAsync_Ciba_Throws_WhenOrgNameClaimMismatch()
    {
        var clientSecret = Guid.NewGuid().ToString("N");
        var idToken = GenerateHs256Token(clientSecret, new List<Claim> { new("org_name", "acme") });

        using var client = BuildClient(BuildMockHandlerReturning(idToken).Object);

        await Assert.ThrowsAsync<IdTokenValidationException>(() =>
            client.GetTokenAsync(
                BuildRequest(clientSecret, JwtSignatureAlgorithm.HS256, organization: "differentcompany")));
    }
}
