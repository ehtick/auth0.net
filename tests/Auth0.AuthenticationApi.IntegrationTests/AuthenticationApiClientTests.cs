using Auth0.Tests.Shared;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Auth0.AuthenticationApi.IntegrationTests;

public class AuthenticationApiClientTests : TestBase
{
    [Fact]
    public async Task Disposes_connection_it_creates_on_dispose()
    {
        var authClient = new AuthenticationApiClient("https://docs.auth0.com");
        authClient.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => authClient.StartPasswordlessSmsFlowAsync(new Models.PasswordlessSmsRequest()));
    }

    [Fact]
    public void Does_not_dispose_connection_it_does_not_create()
    {
        var connection = new FakeConnection();
        var authClient = new AuthenticationApiClient("https://docs.auth0.com", connection);
        authClient.Dispose();
        Assert.False(connection.IsDisposed);
    }

    private class FakeConnection : IAuthenticationConnection, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public Task<T> GetAsync<T>(Uri uri, IDictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(T));
        }

        public Task<T> SendAsync<T>(HttpMethod method, Uri uri, object body, IDictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(T));
        }
    }

    [Fact]
    public void BuildForwardedForHeaders_WithNull_ReturnsNull()
    {
        var result = AuthenticationApiClient.BuildForwardedForHeaders(null);
        result.Should().BeNull();
    }

    [Fact]
    public void BuildForwardedForHeaders_WithEmptyString_ReturnsNull()
    {
        var result = AuthenticationApiClient.BuildForwardedForHeaders(string.Empty);
        result.Should().BeNull();
    }

    [Fact]
    public void BuildForwardedForHeaders_WithValidIPv4_ReturnsHeader()
    {
        var result = AuthenticationApiClient.BuildForwardedForHeaders("192.168.1.1");
        result.Should().NotBeNull();
        result.Should().ContainKey("auth0-forwarded-for");
        result["auth0-forwarded-for"].Should().Be("192.168.1.1");
    }

    [Fact]
    public void BuildForwardedForHeaders_WithValidIPv6_ReturnsHeader()
    {
        var result = AuthenticationApiClient.BuildForwardedForHeaders("2001:db8::1");
        result.Should().NotBeNull();
        result.Should().ContainKey("auth0-forwarded-for");
        result["auth0-forwarded-for"].Should().Be("2001:db8::1");
    }

    [Fact]
    public void BuildForwardedForHeaders_ValidIPv4_HeaderValueMatchesInput()
    {
        var ip = "10.0.0.255";
        var result = AuthenticationApiClient.BuildForwardedForHeaders(ip);
        result["auth0-forwarded-for"].Should().Be(ip);
    }

    [Fact]
    public void BuildForwardedForHeaders_WithHostname_ThrowsArgumentException()
    {
        Action act = () => AuthenticationApiClient.BuildForwardedForHeaders("example.com");
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("forwardedForIp");
    }

    [Fact]
    public void BuildForwardedForHeaders_WithArbitraryString_ThrowsArgumentException()
    {
        Action act = () => AuthenticationApiClient.BuildForwardedForHeaders("not-an-ip");
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("forwardedForIp");
    }
}