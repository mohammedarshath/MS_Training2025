using System.Net.Http.Json;
using PollyUsersDemo.Models;

namespace PollyUsersDemo.Services;

public class JsonPlaceholderClient(HttpClient http, ILogger<JsonPlaceholderClient> logger)
{
    private readonly HttpClient _http = http;
    private readonly ILogger<JsonPlaceholderClient> _logger = logger;

    public async Task<IReadOnlyList<JsonPlaceholderUser>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _http.GetFromJsonAsync<List<JsonPlaceholderUser>>("users", ct);
        return users ?? [];
    }

    public async Task<JsonPlaceholderUser?> GetUserAsync(int id, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<JsonPlaceholderUser>($"users/{id}", ct);
}
