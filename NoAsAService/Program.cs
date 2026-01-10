using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;
using NoAsAService.Models;
using NoAsAService.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load languages
const string defaultLanguage = "en";
FrozenSet<string> languages = Directory.GetFiles("reasons")
    .Select(Path.GetFileNameWithoutExtension)
    .ToFrozenSet()!;
if (!languages.Contains(defaultLanguage))
{
    throw new ApplicationException($"The default language ({defaultLanguage}) does not exist.");    
}

// Read files
var reasons = languages.ToDictionary(
    x => x,
    x => File.ReadAllLines(Path.Combine("reasons", $"{x}.txt"), Encoding.UTF8)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToArray()
);

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

// Configure Json Serialization
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default)
);

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

// Endpoints
var preSerializedJsonValues = PreSerialize();
app.MapGet("/", () => Results.Text(preSerializedJsonValues.EndpointIndex, "application/json", Encoding.UTF8)).RequireRateLimiting("PerIp");
app.MapGet("/langs", () => Results.Text(preSerializedJsonValues.OrderedLanguages, "application/json", Encoding.UTF8)).RequireRateLimiting("PerIp");
app.MapGet("/no", (HttpContext ctx, string? lang = null) =>
{
    // Avoid caching the response
    ctx.Response.Headers["Cache-Control"] = "no-store";
    
    var language = lang ?? defaultLanguage;
    if (!languages.Contains(language))
    {
        return Results.BadRequest("Invalid language");
    }

    var reason = reasons[language][Random.Shared.Next(reasons[language].Length)];
    return Results.Text(reason, "text/plain", Encoding.UTF8, StatusCodes.Status200OK);
}).RequireRateLimiting("PerIp");

app.Run();
return;

// Pre-serialize to reduce on demand CPU usage & latency
PreSerializedJsonValues PreSerialize()
{
    PreSerializedJsonValues result = new()
    {
        OrderedLanguages = JsonSerializer.Serialize(
            languages.OrderBy(x => x).ToArray(),
            AppJsonContext.Default.StringArray
        )
    };

    // Wait for the App to be started before retrieving all the endpoints
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();

        var endpointIndex = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText != "/")
            .Select(e => new EndpointSummary
            {
                Route = e.RoutePattern.RawText!,
                Methods = e.Metadata
                    .OfType<HttpMethodMetadata>()
                    .SelectMany(m => m.HttpMethods)
                    .Distinct()
                    .ToArray()
            })
            .ToArray();

        if (endpointIndex.Length == 0)
        {
            throw new ApplicationException("No endpoints were found.");
        }
    
        result.EndpointIndex = JsonSerializer.Serialize(
            endpointIndex, 
            AppJsonContext.Default.EndpointSummaryArray
        );
    });

    return result;
}