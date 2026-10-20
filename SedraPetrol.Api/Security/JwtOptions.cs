namespace SedraPetrol.Api.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "SedraPetrol";
    public string Audience { get; set; } = "SedraPetrol.Portal";
    /// <summary>At least 32 characters. Override in appsettings / environment for production.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Access token lifetime (short: it cannot be revoked). Default 15 minutes.</summary>
    public int ExpiryMinutes { get; set; } = 15;
    /// <summary>Refresh token lifetime; every use rotates it and starts a new period (sliding). Default 7 days of inactivity.</summary>
    public int RefreshTokenDays { get; set; } = 7;
    /// <summary>Maximum session length from login, whatever the refreshes (re-login after). Default 30 days.</summary>
    public int RefreshTokenMaxDays { get; set; } = 30;
    /// <summary>A just-rotated token presented again within this many seconds (two tabs refreshing together) is refused without killing the session.</summary>
    public int RefreshReuseGraceSeconds { get; set; } = 30;
}
