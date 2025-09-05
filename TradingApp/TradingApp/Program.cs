using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using System;
using System.Data.SqlClient;
using TradingApp.Contexts;
using TradingApp.Graphql.Mutations;
using TradingApp.Graphql.Queries;
using TradingApp.Repositories;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();


// Add services to the container.

builder.Services.AddControllers();



builder.Services.AddDbContext<TraderContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)
    ));

//dependency injection
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IRoleRepository, RoleRepository>();
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});


builder.Services.AddHttpClient("trader",
   client => client.BaseAddress = new Uri("http://traderapi/"))
      .AddServiceDiscovery();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

// GraphQL
builder.Services
    .AddGraphQLServer()
    .RegisterDbContextFactory<TraderContext>()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();
var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection();
app.UseCors(policyName);

app.UseAuthorization();

app.MapControllers();


//Apply migrations at startup (nice for containers/dev)
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<TraderContext>();
//    db.Database.Migrate();
//}
app.MapGraphQL("/graphql");


app.Run();
