using VehicleInfoConsumer.Listeners;
using VehicleInfoConsumer.Repository;
using VehicleInfoConsumer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Bind options
builder.Services.Configure<KafkaConsumerOptions>(
    builder.Configuration.GetSection("Kafka"));

// Message handler (your domain logic)
builder.Services.AddSingleton<IMessageHandler, VehicleInfoHandler>();

// Background Kafka consumer
builder.Services.AddHostedService<VehicleBackgroundService>();
builder.Services.AddTransient<IVehicleRepo,VehicleRepo>();

// Health checks
builder.Services.AddHealthChecks();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok("Kafka Consumer API is running"));

app.MapGet("/consumer/status", (VehicleBackgroundService svc) =>
{
    return Results.Ok(new
    {
        svc.Consumed,
        svc.Succeeded,
        svc.Failed,
        LastMessageUtc = svc.LastMessageAt?.UtcDateTime,
        svc.Paused
    });
});

app.MapPost("/consumer/pause", (VehicleBackgroundService svc) =>
{
    svc.Paused = true;
    return Results.Ok(new { svc.Paused });
});

app.MapPost("/consumer/resume", (VehicleBackgroundService svc) =>
{
    svc.Paused = false;
    return Results.Ok(new { svc.Paused });
});

app.MapHealthChecks("/health");

app.Run();
