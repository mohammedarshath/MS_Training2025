using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Steeltoe.Discovery.Client;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SystemBackend;
using VehicleAPI.Auth;
using VehicleAPI.Contexts;
using VehicleAPI.DTO;
using VehicleAPI.Graphql;
using VehicleAPI.Producers;
using VehicleAPI.Repositories;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
//access config server
builder.Configuration.AddConfigServer();
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    });


//builder.Services.AddDbContext<VehicleContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
//    sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)
//    ));

//Externalize the connection string for use in EF Core CLI tools

// 1) Build base configuration (env, appsettings, etc.)
//var baseConfig = builder.Configuration;

// 2) Read Vault config from env
var vaultAddr = configuration["vaulturl"];
var vaultToken = configuration["roottoken"];
var vaultKvPath = configuration["secretpath"];

// 3) Setup Vault client (Token auth for dev)
IVaultClient? vaultClient = null;
if (!string.IsNullOrWhiteSpace(vaultToken))
{
    var authMethod = new TokenAuthMethodInfo(vaultToken);
    var settings = new VaultClientSettings(vaultAddr, authMethod)
    {
        ContinueAsyncTasksOnCapturedContext = false
    };
    vaultClient = new VaultClient(settings);
}

// 4) Pull secrets once and merge into config (in-memory)
var secrets = new Dictionary<string, string?>();
var user = "";
var password = "";
if (vaultClient != null)
{
    // KV v2: read at /secret/data/myapp (not /secret/myapp)
    var kv = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
     path: configuration["path"],
     mountPoint: configuration["mountpath"]
 );

    // Flatten KV into ASP.NET style keys (supports nested configuration)
    foreach (var kvp in kv.Data.Data)
    {
        secrets[kvp.Key] = kvp.Value?.ToString();
    }
    var data = kv.Data.Data; // Dictionary<string, object?>

    user = data.TryGetValue("username", out var u) ? u?.ToString() : "sa";
    password = data.TryGetValue("password", out var p) ? p?.ToString() : null;
}

Console.WriteLine("Secrets loaded from Vault: " + user);

SqlConnectionStringBuilder providerCs = new SqlConnectionStringBuilder();
providerCs.UserID = user;
providerCs.Password = password;

providerCs.InitialCatalog = configuration["dbname"];
//providerCs.InitialCatalog = "TraderDB";
providerCs.DataSource = "host.docker.internal,1403";
//providerCs.DataSource = configuration["machinename"] + "\\" + configuration["servername"];
//providerCs.DataSource = configuration["username"] + "\\" + configuration["servername"];
providerCs.Encrypt = true;
providerCs.TrustServerCertificate = true;
providerCs.MultipleActiveResultSets = true;

builder.Services.AddDbContext<VehicleContext>(o =>
o.UseSqlServer(providerCs.ToString(), sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));


builder.Services.AddTransient<IVehicleRepo,VehicleRepo>();

// Manual AutoMapper config without the DI helper package
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<VehicleProfile>();
});

//oauth2 setup could go here
var authority = builder.Configuration["Jwt:Authority"]!;
var validateAudience = bool.TryParse(builder.Configuration["Jwt:ValidateAudience"], out var va) && va;
var audience = builder.Configuration["Jwt:Audience"];
if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddAuthentication(TestAuthHandler.Scheme)
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

    builder.Services.AddAuthorization(options =>
    {
        // Example policy based on scopes
        options.AddPolicy("Vehicles.Read", p =>
            p.RequireAssertion(ctx =>
            {
                // check `scope` claims like: "vehicles.read profile"
                var hasScope = ctx.User.Claims
                    .Where(c => c.Type == "scope")
                    .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Any(s => string.Equals(s, "vehicles.read", StringComparison.OrdinalIgnoreCase));

                // allow admin via role
                var isAdmin = ctx.User.IsInRole("admin")
                              || ctx.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "admin");

                return hasScope || isAdmin;
            }));
    });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // where the API can fetch OIDC metadata & JWKS from:
        o.Authority = "http://host.docker.internal:8080/realms/master"; // Windows/Mac
                                                                        // (Linux: use your host IP, e.g. http://172.17.0.1:8080/realms/master)
        o.RequireHttpsMetadata = false;

        // EXACT string that’s inside your JWT `iss`:
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8080/realms/master",
            ValidateAudience = false
        };
    });

    builder.Services.AddAuthorization();

}


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "VehicleAPI", Version = "v1" });
    opt.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(builder.Configuration["SwaggerOAuth:AuthorizationUrl"]!),
                TokenUrl = new Uri(builder.Configuration["SwaggerOAuth:TokenUrl"]!),
                Scopes = new Dictionary<string, string>
                {
                    ["developer"] = "Developer scope"
                }
            }
        }
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } }
        ] = new[] { "developer" }
    });


});
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                                                    new HeaderApiVersionReader("x-api-version"),
                                                    new MediaTypeApiVersionReader("x-api-version"));
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});
var policyName = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: policyName,
                      builder =>
                      {
                          builder
                             .WithOrigins("http://localhost:*", "")
                             //.WithOrigins("http://localhost:3000")
                             // specifying the allowed origin
                             // .WithMethods("GET") // defining the allowed HTTP method
                             .AllowAnyOrigin()
                             // .WithHeaders(HeaderNames.ContentType, "ApiKey")
                             .AllowAnyMethod()
                            .AllowAnyHeader(); // allowing any header to be sent
                      });
});

builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

// Use ControllersWithViews to bring in ViewFeatures & antiforgery filter support
builder.Services.AddControllersWithViews(options =>
{
    // Add the filter only in non-dev to avoid breaking Swagger
    if (!builder.Environment.IsDevelopment())
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

//GraphQL setup
builder.Services
    .AddGraphQLServer()
    .RegisterDbContextFactory<VehicleContext>()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
//eureka connection

builder.Services.AddDiscoveryClient(configuration);

//kafka producer setup
// Bind Kafka options from config
builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka"));

// Register a singleton producer (recommended by Confluent for reuse)
builder.Services.AddSingleton<IVehicleInfoProducer, VehicleInfoProducer>();

// Health checks
builder.Services.AddHealthChecks();



var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.OAuthClientId(builder.Configuration["SwaggerOAuth:ClientId"]);
        ui.OAuthUsePkce();
        ui.OAuthScopes(builder.Configuration["SwaggerOAuth:Scope"]);
    });
}

app.UseHttpsRedirection();
app.UseCors(policyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseMetricServer();
app.MapGet("/vehicles", async () => "ok").RequireAuthorization();
// Public
app.MapGet("/public", () => new { ok = true });

// Require the "developer" scope
app.MapGet("/dev/only", () => new { secret = "hello developer" })
   .RequireAuthorization("developer");
app.MapControllers();
//Apply migrations at startup (nice for containers/dev)
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<VehicleContext>();
//    if (db.Database.GetPendingMigrations().Any())
//    {
//        db.Database.Migrate();
//    }
//}

app.MapGraphQL("/graphql");
// Public

app.Run();


public partial class Program { }