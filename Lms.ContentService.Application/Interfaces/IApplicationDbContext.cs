using Lms.ContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lms.ContentService.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<CourseModule> Modules { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<LessonResource> Resources { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}