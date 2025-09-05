using static System.Net.WebRequestMethods;

namespace CircuitBreaker.Services
{
    public record UserDto(int Id, string Name, string Email);

    // A simple “safe default” or cached provider (replace with Redis/memory)
    
    public sealed class ClientAccess(HttpClient http)
    {
        private readonly HttpClient _http = http;

        public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default)
            => await _http.GetFromJsonAsync<List<User>>("users", ct) ?? [];

        public async Task<User?> GetUserAsync(int id, CancellationToken ct = default)
            => await _http.GetFromJsonAsync<User>($"users/{id}", ct);
        public static Task<UserDto[]> GetFallbackUsersAsync() =>
             Task.FromResult(new[]
             {
        new UserDto(0, "Fallback User", "fallback@example.com")
             });

    }

    public record Geo(string lat, string lng);
    public record Address(string street, string suite, string city, string zipcode, Geo geo);
    public record Company(string name, string catchPhrase, string bs);
    public record User(
        int id, string name, string username, string email,
        Address address, string phone, string website, Company company
    );
}
