namespace TradingApp.Graphql.Queries
{
    // GraphQL/Query.cs
    using HotChocolate;
    using HotChocolate.Data;
    using HotChocolate.Types;
    using Microsoft.EntityFrameworkCore;
    using System;
    using TradingApp.Contexts;
    using TradingApp.Models;

    public class Query
    {
        // IQueryable + EF middleware unlocks paging/filtering/sorting/projections
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<User> GetUsers([Service] TraderContext db) =>
            db.Users.AsNoTracking();

        [UseProjection]
        public Task<User?> GetUserById([Service] TraderContext db, long id) =>
            db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
    }

}
