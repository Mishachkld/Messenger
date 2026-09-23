using Messenger.Api.Features.Auth;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapAuthEndpoint();
app.Run();
