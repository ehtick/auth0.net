﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Auth0.IntegrationTests.Shared.CleanUp;
using Auth0.ManagementApi.IntegrationTests.Testing;
using Auth0.ManagementApi.Models;
using Auth0.Tests.Shared;

using FluentAssertions;
using Xunit;

namespace Auth0.ManagementApi.IntegrationTests;

public class JobsTestsFixture : TestBaseFixture
{
    public Connection TestAuth0Connection;
    public Connection TestEmailConnection;
    public User TestAuth0User;
    public User TestEmailUser;
    private const string Password = "4cX8awB3T%@Aw-R:=h@ae@k?";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Create a connection
        TestAuth0Connection = await ApiClient.Connections.CreateAsync(new ConnectionCreateRequest
        {
            Name = $"{TestingConstants.ConnectionPrefix}-{TestBaseUtils.MakeRandomName()}",
            Strategy = "auth0",
            EnabledClients = new[] { TestBaseUtils.GetVariable("AUTH0_CLIENT_ID"), TestBaseUtils.GetVariable("AUTH0_MANAGEMENT_API_CLIENT_ID") }
        });

        TrackIdentifier(CleanUpType.Connections, TestAuth0Connection.Id);

        TestEmailConnection = await ApiClient.Connections.CreateAsync(new ConnectionCreateRequest
        {
            Name = $"{TestingConstants.ConnectionPrefix}-{TestBaseUtils.MakeRandomName()}",
            Strategy = "email",
            EnabledClients = new[] { TestBaseUtils.GetVariable("AUTH0_CLIENT_ID"), TestBaseUtils.GetVariable("AUTH0_MANAGEMENT_API_CLIENT_ID") }
        });

        TrackIdentifier(CleanUpType.Connections, TestEmailConnection.Id);

        // Create a user
        TestAuth0User = await ApiClient.Users.CreateAsync(new UserCreateRequest
        {
            Connection = TestAuth0Connection.Name,
            Email = $"{Guid.NewGuid():N}{TestingConstants.UserEmailDomain}",
            EmailVerified = true,
            Password = Password
        });

        TrackIdentifier(CleanUpType.Users, TestAuth0User.UserId);

        TestEmailUser = await ApiClient.Users.CreateAsync(new UserCreateRequest
        {
            Connection = TestEmailConnection.Name,
            Email = $"{Guid.NewGuid():N}{TestingConstants.UserEmailDomain}",
            EmailVerified = true,
        });

        TrackIdentifier(CleanUpType.Users, TestEmailUser.UserId);

    }

    public override async Task DisposeAsync()
    {
        foreach (KeyValuePair<CleanUpType, IList<string>> entry in identifiers)
        {
            await ManagementTestBaseUtils.CleanupAsync(ApiClient, entry.Key, entry.Value);
        }

        ApiClient.Dispose();
    }
}

public class JobsTest : IClassFixture<JobsTestsFixture>
{
    private JobsTestsFixture fixture;

    public JobsTest(JobsTestsFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Can_send_verification_email()
    {
        var existingOrganizationId = "org_x2j4mAL75v96wKkt";

        await fixture.ApiClient.Organizations.AddMembersAsync(existingOrganizationId, new OrganizationAddMembersRequest
        {
            Members = new List<string> { fixture.TestAuth0User.UserId }
        });

        var sendVerification = await fixture.ApiClient.Jobs.SendVerificationEmailAsync(new VerifyEmailJobRequest
        {
            UserId = fixture.TestAuth0User.UserId,
            ClientId = TestBaseUtils.GetVariable("AUTH0_CLIENT_ID"),
            OrganizationId = existingOrganizationId
        });
        sendVerification.Should().NotBeNull();
        sendVerification.Id.Should().NotBeNull();

        // Check to see whether we can get the same job again
        var job = await fixture.ApiClient.Jobs.GetAsync(sendVerification.Id);
        job.Should().NotBeNull();
        job.Id.Should().Be(sendVerification.Id);
        job.Type.Should().Be("verification_email");
        job.Status.Should().Be("pending");
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        await fixture.ApiClient.Organizations.DeleteMemberAsync(existingOrganizationId, new OrganizationDeleteMembersRequest
        {
            Members = new List<string> { fixture.TestAuth0User.UserId }
        });
    }

    [Fact]
    public async Task Can_send_verification_email_with_identity()
    {
        var sendVerification = await fixture.ApiClient.Jobs.SendVerificationEmailAsync(new VerifyEmailJobRequest
        {
            UserId = fixture.TestEmailUser.UserId,
            ClientId = TestBaseUtils.GetVariable("AUTH0_CLIENT_ID"),
            Identity = new EmailVerificationIdentity
            {
                Provider = "email",
                UserId = fixture.TestEmailUser.UserId.Replace("email|", "")
            }
        });
        sendVerification.Should().NotBeNull();
        sendVerification.Id.Should().NotBeNull();

        // Check to see whether we can get the same job again
        var job = await fixture.ApiClient.Jobs.GetAsync(sendVerification.Id);
        job.Should().NotBeNull();
        job.Id.Should().Be(sendVerification.Id);
        job.Type.Should().Be("verification_email");
        job.Status.Should().Be("pending");
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact(Skip = "Run Manually")]
    public async Task Can_import_users()
    {
        // Send a user import request
        using (var stream = GetType().Assembly.GetManifestResourceStream("Auth0.ManagementApi.IntegrationTests.user-import-test.json"))
        {
            var importUsers = await fixture.ApiClient.Jobs.ImportUsersAsync(fixture.TestAuth0Connection.Id, "user-import-test.json", stream, sendCompletionEmail: false);
            importUsers.Should().NotBeNull();
            importUsers.Id.Should().NotBeNull();
            importUsers.Type.Should().Be("users_import");
            importUsers.Status.Should().Be("pending");
            importUsers.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            importUsers.ConnectionId.Should().Be(fixture.TestAuth0Connection.Id);
            importUsers.Connection.Should().Be(fixture.TestAuth0Connection.Name);
                
            // Error Details for this job should be null
            var errorDetails = await fixture.ApiClient.Jobs.GetErrorDetailsAsync(importUsers.Id);
            errorDetails.Should().BeNull();
        }
    }

    [Fact(Skip = "Run Manually")]
    public async Task Can_export_users()
    {
        var request = new UsersExportsJobRequest
        {
            ConnectionId = fixture.TestAuth0Connection.Id,
            Format = UsersExportsJobFormat.JSON,
            Limit = 1,
            Fields = new System.Collections.Generic.List<UsersExportsJobField> { new() { Name = "email" } }
        };

        var exportUsers = await fixture.ApiClient.Jobs.ExportUsersAsync(request);
        exportUsers.Should().NotBeNull();
        exportUsers.Id.Should().NotBeNull();
        exportUsers.Type.Should().Be("users_export");
        exportUsers.Status.Should().Be("pending");
        exportUsers.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        exportUsers.ConnectionId.Should().Be(fixture.TestAuth0Connection.Id);
        exportUsers.Connection.Should().Be(fixture.TestAuth0Connection.Name);
            
        // Error Details for this job should be null
        var errorDetails = await fixture.ApiClient.Jobs.GetErrorDetailsAsync(exportUsers.Id);
        errorDetails.Should().BeNull();
    }
        
    [Fact]
    public async Task Parse_Errors_When_Invalid_Users()
    {
        var expectedErrors = new[]
        {
            new JobImportErrorDetails()
            {
                Errors =
                [
                    new Error()
                    {
                        Code = "INVALID_FORMAT",
                        Message =
                            "Error in identities[0].profileData.email property - Object didn't pass validation for format email: john.doe1@nonexistingdomain",
                        Path = "identities[0].profileData.email"
                    }
                ]
            },
            new JobImportErrorDetails()
            {
                Errors =
                [
                    new Error()
                    {
                        Code = "INVALID_FORMAT",
                        Message =
                            "Error in identities[0].profileData.email property - Object didn't pass validation for format email: john.doe2@nonexistingdomain",
                        Path = "identities[0].profileData.email"
                    }
                ]
            }
        };
            
        var importUsers = 
            await SubmitImportJob(
                "Auth0.ManagementApi.IntegrationTests.Data.UsersImportInvalid.json",
                "Data.UsersImportInvalid.json");
            
        // Job Error should not be null since the import will fail due to invalid data
        var errorDetails = await fixture.ApiClient.Jobs.GetErrorDetailsAsync(importUsers.Id);
        errorDetails.Should().NotBeNull();
        errorDetails.JobErrorDetails.Should().BeNull();
        errorDetails.JobImportErrorDetails.Should().NotBeNull();
        errorDetails.JobImportErrorDetails.Length.Should().Be(2);
            
        errorDetails.JobImportErrorDetails.Should().BeEquivalentTo(
            expectedErrors, options => options.Excluding(x => x.User));
    }
        
    [Fact]
    public async Task Parse_Errors_When_Invalid_File()
    {
        var failureReason = "Failed to parse users file JSON when importing users. Make sure it is valid JSON.";
            
        var importUsers = 
            await SubmitImportJob(
                "Auth0.ManagementApi.IntegrationTests.Data.UsersImportInvalidFile.json",
                "Data.UsersImportInvalidFile.json");
            
        // Job Error should not be null since the import will fail due to invalid data
        var errorDetails = await fixture.ApiClient.Jobs.GetErrorDetailsAsync(importUsers.Id);
        errorDetails.Should().NotBeNull();

        errorDetails.JobImportErrorDetails.Should().BeNull();
            
        errorDetails.JobErrorDetails.Id.Should().NotBeNull();
        errorDetails.JobErrorDetails.Type.Should().Be("users_import");
        errorDetails.JobErrorDetails.ConnectionId.Should().Be(fixture.TestAuth0Connection.Id);
        errorDetails.JobErrorDetails.Connection.Should().Be(fixture.TestAuth0Connection.Name);
        errorDetails.JobErrorDetails.Status.Should().Be("failed");
        errorDetails.JobErrorDetails.StatusDetails.Should().Be(failureReason);
    }

    private async Task<Job> SubmitImportJob(string manifestResourceStreamName, string fileName)
    {
        // Send an invalid user import request
        await using var stream = 
            GetType().Assembly.GetManifestResourceStream(manifestResourceStreamName);
        var importUsers = 
            await fixture.ApiClient.Jobs.ImportUsersAsync(
                fixture.TestAuth0Connection.Id, fileName, stream, sendCompletionEmail: false);
            
        // Let the job execute so that it fails
        await Task.Delay(TimeSpan.FromSeconds(5));
            
        importUsers.Should().NotBeNull();
        importUsers.Id.Should().NotBeNull();
        importUsers.Type.Should().Be("users_import");
        importUsers.ConnectionId.Should().Be(fixture.TestAuth0Connection.Id);
        importUsers.Connection.Should().Be(fixture.TestAuth0Connection.Name);
        return importUsers;
    }
}