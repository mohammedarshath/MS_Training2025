using Microsoft.EntityFrameworkCore;



namespace TradingApp.Contexts
{
    public class TraderContext: DbContext
    {
      
        public TraderContext(DbContextOptions<TraderContext> options) : base(options)
        {
           // this.Database.EnsureCreated();
        }
        public DbSet<Models.User> Users { get; set; }
       
        public DbSet<Models.Role> Roles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    j => j
                        .HasOne<Models.Role>()
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_UserRole_RoleId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Models.User>()
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_UserRole_UserId")
                        .OnDelete(DeleteBehavior.ClientCascade));
        }
    }
}
