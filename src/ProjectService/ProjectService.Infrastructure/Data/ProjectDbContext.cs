using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Entities;

namespace ProjectService.Infrastructure.Data;

public class ProjectDbContext : DbContext
{
    public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options) { }

    public DbSet<ProjectItem> Projects => Set<ProjectItem>();
    public DbSet<ProjectTaskItem> Tasks => Set<ProjectTaskItem>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProjectItem>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.HasIndex(p => p.Name);
        });

        builder.Entity<ProjectTaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(200);

            entity.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.ProjectId);
        });

        builder.Entity<TimeEntry>(entity =>
        {
            entity.HasKey(te => te.Id);
            entity.Property(te => te.Notes).HasMaxLength(2000);
            entity.Property(te => te.Hours).HasColumnType("decimal(5,2)");

            entity.HasOne(te => te.Project)
                .WithMany()
                .HasForeignKey(te => te.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(te => te.Task)
                .WithMany()
                .HasForeignKey(te => te.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(te => te.UserId);
            entity.HasIndex(te => te.EntryDate);
            entity.HasIndex(te => te.ProjectId);
            entity.HasIndex(te => te.TaskId);
        });
    }
}
