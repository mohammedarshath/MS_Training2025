using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VehicleAPI.Tests;

public class VehiclesEndpointTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    // Use primary constructor to fix IDE0290
    public VehiclesEndpointTest(WebApplicationFactory<Program> factory) =>
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DOTNET_ENVIRONMENT", "Test");
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Test");
        });

    [Fact]
    public async Task GetVehicles_returns_200_and_json_in_Test_env()
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Bypass", "1");
        client.DefaultRequestHeaders.Add("X-Test-User", "alice");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "admin,reader");
        client.DefaultRequestHeaders.Add("X-Test-Scopes", "vehicles.read profile");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var resp = await client.GetAsync("/api/v1/vehicles");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        // (optional) assert shape: doc.RootElement[0].GetProperty("id").GetInt32().Should().Be(1);
    }
}