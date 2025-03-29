namespace KeycloakLearnIdentity.Api.Models;

public class KeycloakSettings
{
    public string Authority { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string AdminLink { get; set; }
    public string MasterAuthority { get; set; }
    public string AdminUsername { get; set; }
    public string AdminPassword { get; set; }
    public string Audience { get; set; }
}