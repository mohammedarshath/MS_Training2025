using TradingApp.Models;

namespace TradingApp.DTOs
{
    public record UserReadDto(long Id, string UserName,string Email, IEnumerable<Role> Roles);
}
