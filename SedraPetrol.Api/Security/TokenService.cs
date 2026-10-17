using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Security;

public class TokenService
{
    private readonly JwtOptions _opt;
    public TokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

    public (string Token, DateTime ExpiresAt) Create(AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions, IEnumerable<long> branchIds)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(AppClaims.UserId, user.Id.ToString()),
            new(AppClaims.CompanyId, user.CompanyId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.NameEn ?? user.Email),
            new(AppClaims.Branches, string.Join(',', branchIds)),
        };
        if (user.BranchId.HasValue) claims.Add(new Claim(AppClaims.BranchId, user.BranchId.Value.ToString()));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Distinct().Select(p => new Claim(AppClaims.Permission, p)));

        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims, expires: expires, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
