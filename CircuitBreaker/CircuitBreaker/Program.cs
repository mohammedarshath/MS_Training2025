using CircuitBreaker.Services;
using Microsoft.Extensions.Http.Resilience;

using Polly;
using Polly.Fallback;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Typed HttpClient with resilience pipeline (Circuit Breaker + Timeout)
builder.Services
    .AddHttpClient<ClientAccess>(client =>
    {
        client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PollyCircuitBreakerDemo/1.0");
    })
    .AddResilienceHandler("jsonph-pipeline", pipeline =>
    {

        // Define fallback policy
        


        // 1) RETRY — exponential backoff + jitter on transient failures (5xx/408 + common I/O)
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,                       // total tries = 1 + 3
            BackoffType = DelayBackoffType.Exponential, // 200ms, 400ms, 800ms (approx; jittered)
            Delay = TimeSpan.FromMilliseconds(200),
            UseJitter = true,
            ShouldHandle = _ => new ValueTask<bool>(true)
        });

        // CIRCUIT BREAKER:
        // Opens when >=50% of requests fail within a 30s window,
        // only after at least 20 requests have been seen. Stays open 30s.
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 20,
            BreakDuration = TimeSpan.FromSeconds(30),
            // Handles 5xx/408 and common I/O errors by default
           ShouldHandle = _ => new ValueTask<bool>(true)

        });

        // SHORT TIMEOUT per request (bounds latency while breaker is closed/half-open)
        pipeline.AddTimeout(TimeSpan.FromSeconds(2));

        //// 4) OUTGOING RATE LIMITER — token bucket (~10 RPS with burst 10; queue up to 5)
        //pipeline.AddRateLimiter(new RateLimiterStrategyOptions<HttpResponseMessage>
        //{
        //    RateLimiter = PartitionedRateLimiter.Create<HttpRequestMessage, string>(_ =>
        //        RateLimitPartition.GetTokenBucketLimiter("jsonph-global", _ => new TokenBucketRateLimiterOptions
        //        {
        //            TokenLimit = 10,                 // burst size
        //            TokensPerPeriod = 10,            // refill tokens each period
        //            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        //            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        //            QueueLimit = 5,                  // waiting requests allowed
        //            AutoReplenishment = true
        //        }))
        //});
    });



var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


// Minimal API endpoints
app.MapGet("/", () => Results.Redirect("/api/users"));

app.MapGet("/api/users", async (ClientAccess client, CancellationToken ct) =>
{
    var users = await client.GetUsersAsync(ct);
    return Results.Ok(users);
});

app.MapGet("/api/users/{id:int}", async (int id, ClientAccess client, CancellationToken ct) =>
{
    var user = await client.GetUserAsync(id, ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.Run();
