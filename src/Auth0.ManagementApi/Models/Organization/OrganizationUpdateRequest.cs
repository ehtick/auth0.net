﻿using Newtonsoft.Json;

namespace Auth0.ManagementApi.Models;

/// <summary>
/// Requests structure for updating an organization.
/// </summary>
public class OrganizationUpdateRequest
{
    /// <summary>
    /// The display name of the organization
    /// </summary>
    [JsonProperty("display_name")]
    public string DisplayName { get; set; }
        
    /// <summary>
    /// The name of this organization
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
        
    /// <summary>
    /// Organization specific branding settings
    /// </summary>
    [JsonProperty("branding")]
    public OrganizationBranding Branding { get; set; }
        
    /// <summary>
    /// Organization specific metadata
    /// </summary>
    [JsonProperty("metadata")]
    public dynamic Metadata { get; set; }
        
    /// <summary>
    /// This defines the fields that control the token quota
    /// </summary>
    [JsonProperty("token_quota")]
    public TokenQuota TokenQuota { get; set; }
}