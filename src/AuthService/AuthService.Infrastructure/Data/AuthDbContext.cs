using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data;

public class AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AccessTokenAudit> AccessTokenAudits => Set<AccessTokenAudit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.Property(rt => rt.Token).HasMaxLength(200);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(200);
        });

        builder.Entity<AccessTokenAudit>(entity =>
        {
            entity.HasKey(at => at.Id);
            entity.HasIndex(at => at.Token).IsUnique();
            entity.HasIndex(at => at.UserId);
            entity.Property(at => at.Token).HasMaxLength(4000);
        });
    }
}
