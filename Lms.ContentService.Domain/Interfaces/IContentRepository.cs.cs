using Lms.ContentService.Domain.Common;
using Lms.ContentService.Domain.Entities;

namespace Lms.ContentService.Domain.Interfaces;

public interface IContentRepository
{
    // Module operations with pagination
    Task<CourseModule?> GetModuleByIdAsync(Guid moduleId);
    Task<PaginatedList<CourseModule>> GetModulesByCourseIdAsync(int courseId, int pageNumber, int pageSize);
    void AddModule(CourseModule module);
    void UpdateModule(CourseModule module);
    void DeleteModule(CourseModule module);

    // Lesson operations with pagination
    Task<Lesson?> GetLessonByIdAsync(Guid lessonId);
    Task<PaginatedList<Lesson>> GetLessonsByModuleIdAsync(Guid moduleId, int pageNumber, int pageSize, bool publishedOnly = false);
    void AddLesson(Lesson lesson);
    void UpdateLesson(Lesson lesson);

    // Resource operations
    Task<IReadOnlyList<LessonResource>> GetResourcesByLessonIdAsync(Guid lessonId);
    void AddResource(LessonResource resource);
    void RemoveResource(LessonResource resource);
}