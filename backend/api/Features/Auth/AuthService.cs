using Messenger.Api.Common;
using Messenger.Api.Common.Base;

namespace Messenger.Api.Features.Auth;

public class AuthService : BaseService
{
    public Task<ApiResponse<string>> RegisterAsync(UserAuthData userAuthData)
    public Task<ApiResponse<string>> RegisterAsync(RegisterRequest registerRequest)
    {
        // TODO: логику регистрации добавить
        return Task.FromResult(new ApiResponse<string>(string.Empty));
    }
}