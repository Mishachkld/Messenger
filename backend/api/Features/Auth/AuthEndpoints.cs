namespace Messenger.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("auth");
        group.MapPost("/register", RegisterDelegateAsync);
    }

    private static async Task<IResult> RegisterDelegateAsync(HttpContext context, AuthService authService)
    {
        var token = await authService.GenerateTokenAsync();
        return Results.Ok(token);
    }
}