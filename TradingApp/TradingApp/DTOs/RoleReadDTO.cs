namespace TradingApp.DTOs
{
    public record RoleReadDTO(long RoleId, string RoleName, IEnumerable<string> Users);
}
