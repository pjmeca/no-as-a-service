using System.Text;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load languages
const string defaultLanguage = "en";
HashSet<string> languages = Directory.GetFiles("reasons")
    .Select(Path.GetFileNameWithoutExtension)
    .ToHashSet()!;
if (!languages.Contains(defaultLanguage))
{
    throw new ApplicationException($"The default language ({defaultLanguage}) does not exist.");    
}

// Read files
var reasons = languages.ToDictionary(
    x => x,
    x => File.ReadAllLines(Path.Combine("reasons", $"{x}.txt"), Encoding.UTF8)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToArray());

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

// Swagger
#if !NATIVEAOT
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "No-as-a-Service API",
            Version = "v1",
            Description = "No-as-a-Service (NaaS) is a simple API that returns a random rejection reason." +
                          "Use it when you need a realistic excuse, a fun “no”, or want to simulate being turned down in style.",
            Contact = new OpenApiContact()
            {
                Name = "Pablo Meca",
                Email = "hello@pjmeca.com",
                Url = new Uri("https://github.com/pjmeca")
            }
        });
    });
#endif

var app = builder.Build();

// Enable Swagger
#if !NATIVEAOT
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.EnableTryItOutByDefault();
});
#endif

// Use rate limiter
app.UseRateLimiter();

// Endpoint
app.MapGet("/", (string? lang = null) =>
{
    var language = lang ?? defaultLanguage;
    if (!languages.Contains(language))
    {
        return Results.BadRequest("Invalid language");
    }

    var random = new Random();
    var reason = reasons[language][random.Next(reasons[language].Length)];
    return Results.Text(reason, "text/plain", Encoding.UTF8, StatusCodes.Status200OK);
})
.RequireRateLimiting("PerIp");

app.Run();
