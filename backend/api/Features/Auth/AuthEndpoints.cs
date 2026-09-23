namespace Messenger.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("auth");
        group.MapPost("/register", RegisterDelegateAsync);
    }

    private static Task RegisterDelegateAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }
}