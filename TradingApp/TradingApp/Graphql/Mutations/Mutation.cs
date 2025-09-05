namespace TradingApp.Graphql.Mutations
{
    // GraphQL/Mutation.cs
    using HotChocolate;
    using HotChocolate.Types;
    using Microsoft.EntityFrameworkCore;
    using System;
    using TradingApp.Contexts;
    using TradingApp.Models;

    public record CreateUserInput(string Username, string Email, bool IsActive);
    public record UpdateUserInput(int Id, string? Username, string? Email, bool? IsActive);
    public record AddRoleInput(int UserId, string RoleName);

    public class Mutation
    {
        public async Task<User> CreateUser([Service] TraderContext db, CreateUserInput input)
        {
            var user = new User { Username = input.Username, Email = input.Email };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUser([Service] TraderContext db, UpdateUserInput input)
        {
            var user = await db.Users.FindAsync(input.Id);
            if (user is null) return null;

            if (!string.IsNullOrWhiteSpace(input.Username)) user.Username = input.Username!;
            if (!string.IsNullOrWhiteSpace(input.Email)) user.Email = input.Email!;
            //if (input.IsActive is not null) user.IsActive = input.IsActive.Value;

            await db.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUser([Service] TraderContext db, int id)
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return false;
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<User> AddRole([Service] TraderContext db, AddRoleInput input)
        {
            var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.UserId == input.UserId);
            if (user is null) return null;

            var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == input.RoleName)
                       ?? new Role { RoleName = input.RoleName };
            user.Roles.Add(role);
            await db.SaveChangesAsync();
            return user;
        }
    }

}
