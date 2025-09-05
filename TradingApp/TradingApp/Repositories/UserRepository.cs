using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TradingApp.Contexts;
using TradingApp.Models;

namespace TradingApp.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly List<User> _users = new List<User>();
        private TraderContext _context;
        public UserRepository(TraderContext traderContext) {
            _context = traderContext;
        }
        public async Task<User> AddUserAsync(User user)
        {
            var result= await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return result.Entity;

        }

        public async Task<bool> DeleteUserAsync(long userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return false; // User not found
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<IList<User>> GetAllUsers()
        {
            var q = _context.Users
        .AsNoTracking()
        .OrderBy(u => u.UserId);

            // Option A: normal EF Core
            return await q.ToListAsync();
        }

        public async Task<User> GetUserByIdAsync(long userId)
        {
          return await  _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User> UpdateUserAsync(long userId,string email)
        {
            var user1 = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user1 == null)
            {
                return null; // User not found
            }

            
            user1.Email = email;
      
            await _context.SaveChangesAsync();
            return user1;

        }
    }
}
