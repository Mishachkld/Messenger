namespace Messenger.Api.Features.Auth;

public record RegisterRequest(string Name, string Password, string DisplayName);