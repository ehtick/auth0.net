﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using FluentAssertions;
using Xunit;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

using Auth0.AuthenticationApi.Models;
using Auth0.Tests.Shared;

namespace Auth0.AuthenticationApi.IntegrationTests;

public class PasswordlessTests : TestBase
{
    private AuthenticationApiClient authenticationApiClient;
    private readonly string email = "dummy-email@dummy.domain";
    private readonly string phone = "+911234567890";

    public PasswordlessTests()
    {
        authenticationApiClient = new AuthenticationApiClient(GetVariable("AUTH0_AUTHENTICATION_API_URL"));
    }

    [Fact]
    public async Task Can_launch_email_link_flow()
    {
        var request = new PasswordlessEmailRequest
        {
            ClientId = GetVariable("AUTH0_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_CLIENT_SECRET"),
            Email = email,
            Type = PasswordlessEmailRequestType.Link
        };

        var response = await authenticationApiClient.StartPasswordlessEmailFlowAsync(request);
        response.Should().NotBeNull();
        response.Email.Should().Be(request.Email);
        response.Id.Should().NotBeNullOrEmpty();
        response.EmailVerified.Should().NotBeNull();
    }

    [Fact]
    public async Task Can_launch_email_link_flow_with_auth_parameters()
    {
        var request = new PasswordlessEmailRequest
        {
            ClientId = GetVariable("AUTH0_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_CLIENT_SECRET"),
            Email = email,
            Type = PasswordlessEmailRequestType.Link,
            AuthenticationParameters = new Dictionary<string, object>()
            {
                { "response_type", "code" },
                { "scope" , "openid" },
                { "nonce" , "mynonce" },
                { "redirect_uri", "http://localhost:5000/signin-auth0" }
            }
        };

        var response = await authenticationApiClient.StartPasswordlessEmailFlowAsync(request);
        response.Should().NotBeNull();
        response.Email.Should().Be(request.Email);
        response.Id.Should().NotBeNullOrEmpty();
        response.EmailVerified.Should().NotBeNull();
    }

    [Fact]
    public async Task Can_launch_email_code_flow()
    {
        var request = new PasswordlessEmailRequest
        {
            ClientId = GetVariable("AUTH0_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_CLIENT_SECRET"),
            Email = email,
            Type = PasswordlessEmailRequestType.Code
        };
        var response = await authenticationApiClient.StartPasswordlessEmailFlowAsync(request);
        response.Should().NotBeNull();
        response.Email.Should().Be(request.Email);
        response.Id.Should().NotBeNullOrEmpty();
        response.EmailVerified.Should().NotBeNull();
    }

    [SkippableFact(Skip = "Requires SPA application setup")]
    public async Task Can_launch_email_code_flow_for_SPA()
    {
        var request = new PasswordlessEmailRequest
        {
            ClientId = GetVariable("AUTH0_SPA_CLIENT_ID"),
            Email = email,
            Type = PasswordlessEmailRequestType.Code,
            AuthenticationParameters = new Dictionary<string, object>
            {
                ["audience"] = "settings.Audience",
                ["scope"] = "openid profile email offline_access",
            }
        };
        var response = await authenticationApiClient.StartPasswordlessEmailFlowAsync(request);
        response.Should().NotBeNull();
        response.Email.Should().Be(request.Email);
        response.Id.Should().NotBeNullOrEmpty();
        response.EmailVerified.Should().NotBeNull();
    }

    [SkippableFact(Skip = "Requires PhoneProvider setup")]
    public async Task Can_launch_sms_flow()
    {
        // Act
        var request = new PasswordlessSmsRequest
        {
            ClientId = GetVariable("AUTH0_SPA_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_SPA_CLIENT_SECRET"),
            PhoneNumber = phone
        };
        var response = await authenticationApiClient.StartPasswordlessSmsFlowAsync(request);
        response.Should().NotBeNull();
        response.PhoneNumber.Should().Be(request.PhoneNumber);
    }

    [Fact(Skip = "Involves sending message - receiving code - Using that in test case")]
    public async Task Can_exchange_sms_code_for_access_token()
    {
        // Exchange the code
        var token = await authenticationApiClient.GetTokenAsync(new PasswordlessSmsTokenRequest
        {
            ClientId = GetVariable("AUTH0_PASSWORDLESSDEMO_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_PASSWORDLESSDEMO_CLIENT_SECRET"),
            Code = "...",
            Audience = GetVariable("AUTH0_MANAGEMENT_API_AUDIENCE"),
            Scope = "openid email",
            PhoneNumber = phone

        });

        // Assert
        token.Should().NotBeNull();
    }

    [Fact(Skip = "Involves sending an email - receiving a code - using that code to run test")]
    public async Task Can_exchange_email_code_for_access_token()
    {
        // Exchange the code
        var token = await authenticationApiClient.GetTokenAsync(new PasswordlessEmailTokenRequest
        {
            ClientId = GetVariable("AUTH0_SPA_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_SPA_CLIENT_SECRET"),
            Code = "...",
            Audience = GetVariable("AUTH0_MANAGEMENT_API_AUDIENCE"),
            Scope = "openid email",
            Email = email

        });

        // Assert
        token.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Request_a_token_using_sms_code()
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var response = new AccessTokenResponse();
        var domain = GetVariable("AUTH0_AUTHENTICATION_API_URL");
        var code = "AaBhdAOl4OKvjX2I";
        var expectedParams = new Dictionary<string, string>
        {
            { "realm", "sms" },
            { "grant_type", "http://auth0.com/oauth/grant-type/passwordless/otp" },
            { "otp", code },
            { "client_id", GetVariable("AUTH0_CLIENT_ID") },
            { "client_secret", GetVariable("AUTH0_CLIENT_SECRET") },
            { "audience", GetVariable("AUTH0_MANAGEMENT_API_AUDIENCE") },
            { "username", "0123456789" },
            { "scope", "openid email" }
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == $"https://{domain}/oauth/token" && ValidateRequestContent(req, expectedParams)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var authenticationApiClient = new AuthenticationApiClient(domain, new HttpClientAuthenticationConnection(httpClient));

        // Exchange the code
        var token = await authenticationApiClient.GetTokenAsync(new PasswordlessSmsTokenRequest
        {
            ClientId = GetVariable("AUTH0_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_CLIENT_SECRET"),
            Code = code,
            Audience = GetVariable("AUTH0_MANAGEMENT_API_AUDIENCE"),
            Scope = "openid email",
            PhoneNumber = "0123456789"

        });

        // Assert
        token.Should().NotBeNull();
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_Request_a_token_using_email_code()
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var response = new AccessTokenResponse();
        var domain = GetVariable("AUTH0_AUTHENTICATION_API_URL");
        var code = "AaBhdAOl4OKvjX2I";
        var expectedParams = new Dictionary<string, string>
        {
            { "realm", "email" },
            { "grant_type", "http://auth0.com/oauth/grant-type/passwordless/otp" },
            { "otp", code },
            { "client_id", GetVariable("AUTH0_CLIENT_ID") },
            { "client_secret", GetVariable("AUTH0_CLIENT_SECRET") },
            { "audience", GetVariable("AUTH0_MANAGEMENT_API_AUDIENCE") },
            { "username", "test@fake.com" },
            { "scope", "openid email" }
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == $"https://{domain}/oauth/token" && ValidateRequestContent(req, expectedParams)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var authenticationApiClient = new AuthenticationApiClient(domain, new HttpClientAuthenticationConnection(httpClient));

        // Exchange the code
        var token = await authenticationApiClient.GetTokenAsync(new PasswordlessEmailTokenRequest
        {
            ClientId = GetVariable("AUTH0_CLIENT_ID"),
            ClientSecret = GetVariable("AUTH0_CLIENT_SECRET"),
            Code = code,
            Audience = GetVariable("AUTH0_MANAGEMENT_API_AUDIENCE"),
            Scope = "openid email",
            Email = "test@fake.com"

        });

        // Assert
        token.Should().NotBeNull();
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    private bool ValidateRequestContent(HttpRequestMessage content, Dictionary<string, string> contentParams)
    {
        string jsonContent = content.Content.ReadAsStringAsync().Result;
        var result = jsonContent.Split("&").ToDictionary(keyValue => keyValue.Split("=")[0], keyValue => HttpUtility.UrlDecode(keyValue.Split("=")[1]));

        return contentParams.Aggregate(true, (acc, keyValue) => acc && result.GetValueOrDefault(keyValue.Key) == keyValue.Value);
    }
}