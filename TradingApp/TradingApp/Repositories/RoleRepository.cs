using Microsoft.EntityFrameworkCore;
using TradingApp.Contexts;
using TradingApp.Models;

namespace TradingApp.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly List<Role> _roles = new List<Role>();
        private TraderContext _context;
        public RoleRepository(TraderContext traderContext) {
            _context = traderContext;

        }

        public async Task<Role> AddRoleAsync(Role role)
        {
           var result = await _context.Roles.AddAsync(role);
           await  _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<bool> DeleteRoleAsync(long roleId)
        {
            Role role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return false; // Role not found
            }
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<IList<Role>> GetAllRoles()
        {
           return await  _context.Roles.ToListAsync();
        }

        public async Task<Role> UpdateRoleAsync(long roleId, string roleName)
        {
            Role role =  await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return null;
            }
            
            role.RoleName = roleName;
            await _context.SaveChangesAsync();
            return role;
        }
    }
}
