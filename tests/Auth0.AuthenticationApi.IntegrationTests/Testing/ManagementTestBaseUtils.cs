﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth0.IntegrationTests.Shared.CleanUp;
using Auth0.ManagementApi;

namespace Auth0.AuthenticationApi.IntegrationTests.Testing;

public class ManagementTestBaseUtils
{
    public static async Task CleanupAsync(ManagementApiClient client, CleanUpType type, IList<string> identifiers)
    {
        var strategies = new List<CleanUpStrategy>
        {
            new ActionsCleanUpStrategy(client),
            new ClientGrantsCleanUpStrategy(client),
            new ClientsCleanUpStrategy(client),
            new ConnectionsCleanUpStrategy(client),
            new HooksCleanUpStrategy(client),
            new OrganizationsCleanUpStrategy(client),
            new ResourceServersCleanUpStrategy(client),
            new UsersCleanUpStrategy(client),
            new RulesCleanUpStrategy(client),
            new LogStreamsCleanUpStrategy(client),
            new RolesCleanUpStrategy(client),
            new EncryptionKeysCleanupStrategy(client),
            new SelfServiceProviderCleanUpStrategy(client),
            new FormsCleanUpStrategy(client),
            new FlowsCleanUpStrategy(client)
        };

        var cleanUpStrategy = strategies.Single(s => s.Type == type);

        foreach (var identifier in identifiers)
        {
            await cleanUpStrategy.Run(identifier);
        }
    }
}