using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Load from JSON
HashSet<string> languages = ["en", "es"];
var reasons = languages.ToDictionary(
    x => x,
    x => JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine("reasons", $"{x}.json")))!);

// Configure rate-limit: 120 req/min by IP (or CF-Connecting-IP)
builder.Services.AddRateLimiter(options =>
{
    const int permitLimit = 120;
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429; // HTTP 429
        context.HttpContext.Response.ContentType = "application/json";

        var payload = $"Too many requests, please try again later. ({permitLimit} reqs/min/IP)";
        await context.HttpContext.Response.WriteAsJsonAsync(payload, cancellationToken: token);
    };

    options.AddPolicy("PerIp", context =>
    {
        var clientIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                       ?? context.Connection.RemoteIpAddress?.ToString()
                       ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// Use rate limiter
app.UseRateLimiter();

// Endpoint
app.MapGet("/", (string? lang = null) =>
{
    var language = lang ?? languages.First();
    if (!languages.Contains(language))
    {
        return Results.BadRequest("Invalid language");
    }

    var random = new Random();
    var reason = reasons[language][random.Next(reasons[language].Length)];
    return Results.Ok(reason);
})
.RequireRateLimiting("PerIp");

app.Run();
