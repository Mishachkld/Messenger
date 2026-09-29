namespace Messenger.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("auth");
        group.MapPost("/register", RegisterDelegateAsync);
    }

    private static async Task<IResult> RegisterDelegateAsync(
        HttpContext context, 
        AuthService authService,
        RegisterRequest registerRequest)
    {
        var token = await authService.RegisterAsync(registerRequest);
        return Results.Ok(token);
    }
}