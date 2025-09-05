using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Steeltoe.Discovery.Client;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System.Text.Json;
using System.Text.Json.Serialization;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VehicleAPI.Contexts;
using VehicleAPI.DTO;
using VehicleAPI.Graphql;
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
var vaultToken = configuration["rootToken"];
var vaultKvPath = configuration["secretPath"];

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

//Console.WriteLine("Secrets loaded from Vault: " + user);

SqlConnectionStringBuilder providerCs = new SqlConnectionStringBuilder();
providerCs.UserID = user;
providerCs.Password = password;

//providerCs.InitialCatalog = "VehicleDB";
providerCs.InitialCatalog = configuration["dbname"];

//providerCs.InitialCatalog = "TraderDB";
providerCs.DataSource = configuration["username"] + "\\" + configuration["servername"];
//providerCs.DataSource = @"DESKTOP-UVVCE5A\SQLEXPRESS";
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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "TraderAPI", Version = "v1" });


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

//Eureka Service connection
builder.Services.AddDiscoveryClient(configuration);

var app = builder.Build();


app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(policyName);

app.UseAuthorization();

app.MapControllers();
//Apply migrations at startup (nice for containers/dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VehicleContext>();
    //if (db.Database.GetPendingMigrations().Any())
    //{
    //    db.Database.Migrate();
    //}
}

app.MapGraphQL("/graphql");

app.Run();
