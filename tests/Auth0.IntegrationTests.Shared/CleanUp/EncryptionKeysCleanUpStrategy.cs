﻿using System;
using System.Threading.Tasks;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models.Actions;
using Auth0.ManagementApi.Paging;
using static System.Collections.Specialized.BitVector32;

namespace Auth0.IntegrationTests.Shared.CleanUp
{
    public class EncryptionKeysCleanupStrategy : CleanUpStrategy
    {
        public EncryptionKeysCleanupStrategy(ManagementApiClient apiClient) : base(CleanUpType.EncryptionKeys, apiClient)
        {
                
        }

        public override async Task Run(string id)
        {
            System.Diagnostics.Debug.WriteLine("Running EncryptionKeysCleanupStrategy");
            await ApiClient.Keys.DeleteEncryptionKeyAsync(id);
        }
    }
}