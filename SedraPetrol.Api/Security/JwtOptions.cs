namespace SedraPetrol.Api.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "SedraPetrol";
    public string Audience { get; set; } = "SedraPetrol.Portal";
    /// <summary>At least 32 characters. Override in appsettings / environment for production.</summary>
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 720;
}
