using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Messenger.Api.Common;
using Messenger.Api.Common.Base;
using Microsoft.IdentityModel.Tokens;

namespace Messenger.Api.Features.Auth;

public class AuthService : BaseService
{
    private const string Issuer = "ISSUER";
    private const string Audience = "AUDIENCE";
    private const string KEY = "mysupersecret_secretsecretsecretkey!123"; // ключ для шифрации

    public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));

    public Task<ApiResponse<string>> RegisterAsync(RegisterRequest registerRequest)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, registerRequest.Name),
        };
        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(2)),
            signingCredentials: new SigningCredentials(GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return Task.FromResult(new ApiResponse<string>(token));
    }
}