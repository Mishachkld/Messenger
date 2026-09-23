using System.IdentityModel.Tokens.Jwt;
using Messenger.Api.Common.Base;

namespace Messenger.Api.Features.Auth;

public class AuthService : BaseService
{
    public Task<JwtSecurityToken> RegisterAsync(UserAuthData userAuthData)
    {
        // TODO: логику регистрации добавить
        return Task.FromResult(new JwtSecurityToken());
    }
}