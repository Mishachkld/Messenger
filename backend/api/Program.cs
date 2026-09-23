using Messenger.Api.Features.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<AuthService>();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapAuthEndpoint();
app.Run();
