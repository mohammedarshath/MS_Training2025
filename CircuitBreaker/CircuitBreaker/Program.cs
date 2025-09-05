using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Fallback;
using Polly.RateLimiting;
using Polly.Retry;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.RateLimiting;
using Steeltoe.Discovery.Client;
ResiliencePropertyKey<ILogger> LoggerKey = new("logger");
var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
builder.Logging.AddFilter("Polly", LogLevel.Error);
builder.Logging.AddFilter("Microsoft.Extensions.Http.Resilience", LogLevel.Error);
builder.Logging.AddFilter("Microsoft.Extensions.Http.Resilience.ResilienceHandler", LogLevel.Error);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.jsonApi", LogLevel.Warning);

builder.AddServiceDefaults();
//HttpClient with Circuit Breaker
// Register HttpClient with resilience pipeline
builder.Services.AddHttpClient("jsonApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:1502/api/v1/Vehicles");
})
.AddResilienceHandler("resilience", pipeline =>
{
    // Fallback: return safe default when all else fails
    pipeline.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = static args =>
        {
            if (args.Outcome.Exception is not null) return PredicateResult.True();
            if (args.Outcome.Result is HttpResponseMessage r)
            {
                var code = (int)r.StatusCode;
                return code >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout || code == 429
                    ? PredicateResult.True()
                    : PredicateResult.False();
            }
            return PredicateResult.False();
        },

        FallbackAction = static _ =>
        {
            // No exception details; include a correlation id instead
            var errId = Guid.NewGuid().ToString("N");

            var resp = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            resp.Headers.TryAddWithoutValidation("X-From-Fallback", "true");
            resp.Headers.TryAddWithoutValidation("X-Error-Id", errId);

            resp.Content = new StringContent(
                $$"""{"fallback":true,"message":"Upstream temporarily unavailable. Please try again.","errorId":"{{errId}}"}""",
                Encoding.UTF8,
                "application/json");

            return Outcome.FromResultAsValueTask(resp);
        }
    });




    // Rate limiter: allow 5 requests per second
    // Inside your pipeline setup
    pipeline.AddRateLimiter(new HttpRateLimiterStrategyOptions
    {
        // Built-in simple limiter (outbound concurrency)
        DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
        {
            PermitLimit = 5,
            QueueLimit = 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }
    });
    // Retry: 3 attempts, exponential backoff
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        // (Optional – defaults already handle transients)
        ShouldHandle = static args =>
            HttpClientResiliencePredicates.IsTransient(args.Outcome)
                ? PredicateResult.True()
                : PredicateResult.False(),

        // This proves retries happen
        OnRetry = static args =>
        {
            var reason = args.Outcome.Exception?.GetType().Name
                         ?? (args.Outcome.Result is HttpResponseMessage r
                             ? $"{(int)r.StatusCode} {r.ReasonPhrase}"
                             : "unknown");

            Console.WriteLine($"[Retry] attempt {args.AttemptNumber} after {args.Duration} because {reason}");
            return default; // ValueTask.CompletedTask
        }
    });

    // Circuit breaker: open after 5 errors, reset after 30s
    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,              // 50% failure rate
        MinimumThroughput = 5,           // at least 5 calls
        SamplingDuration = TimeSpan.FromSeconds(10),
        BreakDuration = TimeSpan.FromSeconds(30),
        ShouldHandle = static args =>
                HttpClientResiliencePredicates.IsTransient(args.Outcome)
                    ? PredicateResult.True()
                    : PredicateResult.False()
    });
});

//eureka connection

builder.Services.AddDiscoveryClient(configuration);



builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Users API",
        Version = "v1",
        
    });

    // Include XML comments if enabled in csproj
    var xml = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath))
        o.IncludeXmlComments(xmlPath);

    // (optional) enable [SwaggerOperation]/[SwaggerResponse] attributes
    // o.EnableAnnotations();
});


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// (Optional) redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();




app.Run();
