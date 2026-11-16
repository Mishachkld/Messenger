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
    private readonly AuthRepository _authRepository;

    public AuthService(AuthRepository authRepository)
    {
        _authRepository = authRepository;
    }


    public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));

    public async Task<ApiResponse<string>> RegisterAsync(RegisterRequest registerRequest)
    {
        try
        {
            await _authRepository.CreateUserAsync(registerRequest);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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
        return new ApiResponse<string>(token);
    }
}