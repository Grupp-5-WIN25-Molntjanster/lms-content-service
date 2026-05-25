using Lms.ContentService.Application.Interfaces;
using Lms.ContentService.Domain.Entities;
using Lms.ContentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lms.ContentService.Infrastructure.Persistence;

public class ContentDbContext : DbContext, IApplicationDbContext
{
    public DbSet<CourseModule> Modules => Set<CourseModule>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonResource> Resources => Set<LessonResource>();

    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Content");

        modelBuilder.Entity<CourseModule>(entity =>
        {
            entity.ToTable("Modules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => new { e.CourseId, e.Order }).IsUnique();
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("Lessons");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion(v => v.ToString(), v => Enum.Parse<LessonStatus>(v)).HasMaxLength(20);
            entity.HasOne(e => e.Module).WithMany(m => m.Lessons).HasForeignKey(e => e.ModuleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ModuleId, e.Order }).IsUnique();
        });

        modelBuilder.Entity<LessonResource>(entity =>
        {
            entity.ToTable("LessonResources");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResourceType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Lesson).WithMany(l => l.Resources).HasForeignKey(e => e.LessonId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.LessonId, e.FileId }).IsUnique();
        });
    }
}