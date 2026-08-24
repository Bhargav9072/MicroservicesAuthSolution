using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.AuthUserId).IsUnique();
            entity.HasIndex(p => p.Email).IsUnique();
            entity.Property(p => p.FullName).HasMaxLength(200);
            entity.Property(p => p.Email).HasMaxLength(256);
            entity.Property(p => p.Role).HasMaxLength(50);
        });
    }
}
