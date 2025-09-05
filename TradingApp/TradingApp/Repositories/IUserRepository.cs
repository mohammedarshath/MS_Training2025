using TradingApp.Models;

namespace TradingApp.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(long userId);
        Task<User> AddUserAsync(User user);
        Task<User> UpdateUserAsync(long userId, string email);
        Task<bool> DeleteUserAsync(long userId);
        Task<IList<User>> GetAllUsers();
    }
}
