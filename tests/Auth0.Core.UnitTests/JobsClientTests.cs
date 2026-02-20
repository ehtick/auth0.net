using Auth0.ManagementApi.Clients;
using Auth0.ManagementApi.Models;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Auth0.ManagementApi;
using Newtonsoft.Json;
using Xunit;

namespace Auth0.Core.UnitTests;

public class JobsClientTests
{
    private static JobsClient CreateClient(string responseJson)
    {
        var mockConnection = new Mock<IManagementConnection>();
        mockConnection
            .Setup(c => c.GetAsync<string>(
                It.IsAny<Uri>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<JsonConverter[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        return new JobsClient(
            mockConnection.Object,
            new Uri("https://test.auth0.com"),
            new Dictionary<string, string>());
    }

    [Fact]
    public async Task GetErrorDetailsAsync_Returns_Null_When_Response_Is_Empty_Array()
    {
        var client = CreateClient("[]");

        var result = await client.GetErrorDetailsAsync("job_123");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetErrorDetailsAsync_Returns_Null_When_Response_Is_Null_Or_Empty()
    {
        var client = CreateClient(null);

        var result = await client.GetErrorDetailsAsync("job_123");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetErrorDetailsAsync_Returns_JobImportErrorDetails_When_Response_Is_Array_With_Errors()
    {
        var json = """
            [
              {
                "user": { "email": "john@example.com" },
                "errors": [
                  { "code": "INVALID_FORMAT", "message": "Invalid email", "path": "email" }
                ]
              }
            ]
            """;
        var client = CreateClient(json);

        var result = await client.GetErrorDetailsAsync("job_123");

        result.Should().NotBeNull();
        result!.JobImportErrorDetails.Should().NotBeNull();
        result.JobImportErrorDetails!.Length.Should().Be(1);
        result.JobImportErrorDetails[0].Errors![0].Code.Should().Be("INVALID_FORMAT");
        result.JobErrorDetails.Should().BeNull();
    }

    [Fact]
    public async Task GetErrorDetailsAsync_Returns_JobErrorDetails_When_Response_Is_Object()
    {
        var json = """
            {
              "status": "failed",
              "type": "users_import",
              "id": "job_abc",
              "status_details": "Failed to parse users file JSON when importing users."
            }
            """;
        var client = CreateClient(json);

        var result = await client.GetErrorDetailsAsync("job_123");

        result.Should().NotBeNull();
        result!.JobErrorDetails.Should().NotBeNull();
        result.JobErrorDetails!.StatusDetails.Should().Be("Failed to parse users file JSON when importing users.");
        result.JobImportErrorDetails.Should().BeNull();
    }
}
