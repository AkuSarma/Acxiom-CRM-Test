using System.IdentityModel.Tokens.Jwt;

namespace AcxiomCRM.Services;

public class JwtTokenService(IConfiguration cfg) {
    public (string Token, DateTime ExpiresAt) GenerateToken(AppUser user, IList<string> roles) {
        var keyStr = cfg["Jwt:Key"] ?? "AcxiomCRM-Dev-Signing-Key-Change-In-Production-2026!";
        var issuer = cfg["Jwt:Issuer"] ?? "AcxiomCRM";
        var audience = cfg["Jwt:Audience"] ?? "AcxiomCRM.Clients";
        var hours = int.TryParse(cfg["Jwt:ExpiryHours"], out var h) ? h : 8;

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email ?? user.UserName ?? ""),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("FullName", user.FullName ?? "")
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(hours);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
