using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using PollyUsersDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<JsonPlaceholderClient>(client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PollyUsersDemo/1.0");
})
.AddResilienceHandler("jsonph-pipeline", pipeline =>
{
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 5,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromMilliseconds(200),
        ShouldHandle = HttpClientResiliencePredicates.Standard
    });

    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 20,
        BreakDuration = TimeSpan.FromSeconds(30),
        ShouldHandle = HttpClientResiliencePredicates.Standard
    });

    pipeline.AddTimeout(TimeSpan.FromSeconds(2));

    pipeline.AddRateLimiter(new RateLimiterStrategyOptions<HttpResponseMessage>
    {
        RateLimiter = PartitionedRateLimiter.Create<HttpRequestMessage, string>(_ =>
            RateLimitPartition.GetTokenBucketLimiter("jsonph-global",
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5,
                    AutoReplenishment = true
                }))
    });
});

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/api/users"));

app.MapGet("/api/users", async (JsonPlaceholderClient client, CancellationToken ct) =>
{
    var users = await client.GetUsersAsync(ct);
    return Results.Ok(users);
});

app.MapGet("/api/users/{id:int}", async (int id, JsonPlaceholderClient client, CancellationToken ct) =>
{
    var user = await client.GetUserAsync(id, ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapGet("/api/test/burst", async (JsonPlaceholderClient client) =>
{
    var tasks = Enumerable.Range(1, 30).Select(_ => client.GetUsersAsync());
    await Task.WhenAll(tasks);
    return Results.Ok(new { message = "Burst test completed (check logs)" });
});

app.Run();
