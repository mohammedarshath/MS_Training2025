using TradingApp.Models;

namespace TradingApp.Repositories
{
    public interface IRoleRepository
    {
       
        Task<Role> AddRoleAsync(Role role);
        Task<Role> UpdateRoleAsync(long roleId, string roleName);
        Task<bool> DeleteRoleAsync(long roleId);
        Task<IList<Role>> GetAllRoles();
    }
}
