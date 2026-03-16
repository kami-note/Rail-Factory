using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using RailFactory.IAM.Ports;
using RailFactory.Shared.Jwt;

namespace RailFactory.IAM.Infrastructure;

/// <summary>
/// Issues JWTs with tenant_id and roles for downstream services (doc 13, Phase 1).
/// </summary>
public sealed class JwtIssuer : IJwtIssuer
{
    private readonly JwtIssuerOptions _options;

    public JwtIssuer(IOptions<JwtIssuerOptions> options)
    {
        _options = options.Value;
    }

    public string IssueToken(JwtIssueRequest request)
    {
        var now = DateTime.UtcNow;
        var validFor = request.ValidFor ?? _options.DefaultValidFor;
        var expires = now.Add(validFor);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Subject),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (!string.IsNullOrEmpty(request.Name))
            claims.Add(new Claim(JwtIssuerOptions.ClaimName, request.Name));

        // Single tenant for this token (downstream resolver reads this)
        if (!string.IsNullOrEmpty(request.TenantId))
            claims.Add(new Claim(JwtClaimNames.TenantId, request.TenantId));

        // All tenants (e.g. Matrix Admin); optional
        if (request.TenantIds is { Count: > 0 })
            foreach (var tid in request.TenantIds)
                claims.Add(new Claim(JwtClaimNames.TenantIds, tid));

        foreach (var role in request.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Configuration for JWT issuance (RNF-02: keep key in config/secrets, not in code).
/// </summary>
public sealed class JwtIssuerOptions
{
    public const string SectionName = "Jwt";
    public const string ClaimName = "name";

    public string Issuer { get; set; } = "RailFactory.IAM";
    public string Audience { get; set; } = "RailFactory.Services";
    public string SigningKey { get; set; } = ""; // min 32 chars for HS256
    public int DefaultValidForMinutes { get; set; } = 60;
    public TimeSpan DefaultValidFor => TimeSpan.FromMinutes(DefaultValidForMinutes);
}
