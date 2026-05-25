using Lms.ContentService.Domain.Common;
using Lms.ContentService.Domain.Entities;
using Lms.ContentService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lms.ContentService.Infrastructure.Persistence;

public class ContentRepository : IContentRepository
{
    private readonly ContentDbContext _context;
    public ContentRepository(ContentDbContext context) => _context = context;

    public async Task<CourseModule?> GetModuleByIdAsync(Guid moduleId) =>
        await _context.Modules.Include(m => m.Lessons).ThenInclude(l => l.Resources).FirstOrDefaultAsync(m => m.Id == moduleId);

    public async Task<PaginatedList<CourseModule>> GetModulesByCourseIdAsync(Guid courseId, int pageNumber, int pageSize)
    {
        var query = _context.Modules.Where(m => m.CourseId == courseId).OrderBy(m => m.Order);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).Include(m => m.Lessons).ThenInclude(l => l.Resources).ToListAsync();
        return new PaginatedList<CourseModule>(items, totalCount, pageNumber, pageSize);
    }

    public void AddModule(CourseModule module) => _context.Modules.Add(module);
    public void UpdateModule(CourseModule module) => _context.Modules.Update(module);
    public void DeleteModule(CourseModule module) => _context.Modules.Remove(module);

    public async Task<Lesson?> GetLessonByIdAsync(Guid lessonId) =>
        await _context.Lessons.Include(l => l.Resources).FirstOrDefaultAsync(l => l.Id == lessonId);

    public async Task<PaginatedList<Lesson>> GetLessonsByModuleIdAsync(Guid moduleId, int pageNumber, int pageSize, bool publishedOnly = false)
    {
        var query = _context.Lessons.Where(l => l.ModuleId == moduleId);
        if (publishedOnly) query = query.Where(l => l.Status == Domain.Enums.LessonStatus.Published);
        query = query.OrderBy(l => l.Order);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).Include(l => l.Resources).ToListAsync();
        return new PaginatedList<Lesson>(items, totalCount, pageNumber, pageSize);
    }

    public void AddLesson(Lesson lesson) => _context.Lessons.Add(lesson);
    public void UpdateLesson(Lesson lesson) => _context.Lessons.Update(lesson);

    public async Task<IReadOnlyList<LessonResource>> GetResourcesByLessonIdAsync(Guid lessonId) =>
        await _context.Resources.Where(r => r.LessonId == lessonId).ToListAsync();

    public void AddResource(LessonResource resource) => _context.Resources.Add(resource);
    public void RemoveResource(LessonResource resource) => _context.Resources.Remove(resource);
}