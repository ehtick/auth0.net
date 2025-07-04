using System.Runtime.Serialization;

namespace Auth0.ManagementApi.Models.Connections;

/// <summary>
/// When using an external IdP, this flag determines whether 'name', 'given_name', 'family_name', 'nickname',
/// and 'picture' attributes are updated.
/// In addition, it also determines whether the user is created when user doesnt exist previously.
/// Possible values are 'on_each_login' (default value, it configures the connection to automatically create
/// the user if necessary and update the root attributes from the external IdP with each user login.
/// When this setting is used, root attributes cannot be independently updated),
/// 'on_first_login' (configures the connection to create the user and set the root attributes on first login only,
/// allowing them to be independently updated thereafter), and 'never_on_login' (configures the connection not to
/// create the user and not to set the root attributes from the external IdP,
/// allowing them to be independently updated).
/// </summary>
public enum SetUserRootAttributes
{
    [EnumMember(Value = "on_each_login")]
    OnEachLogin,
        
    [EnumMember(Value = "on_first_login")]
    OnFirstLogin,
        
    [EnumMember(Value = "never_on_login")]
    NeverOnLogin
}