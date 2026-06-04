using Lms.ContentService.Application.Common;
using Lms.ContentService.Application.DTOs;
using Lms.ContentService.Application.Interfaces;
using Lms.ContentService.Domain.Entities;
using Lms.ContentService.Domain.Interfaces;

namespace Lms.ContentService.Application.Services;

public class ContentService
{
    private readonly IContentRepository _repository;
    private readonly IFileServiceClient _fileServiceClient;
    private readonly IApplicationDbContext _context;

    public ContentService(IContentRepository repository, IFileServiceClient fileServiceClient, IApplicationDbContext context)
    {
        _repository = repository;
        _fileServiceClient = fileServiceClient;
        _context = context;
    }

    // ================================================================
    // MODULE OPERATIONS WITH PAGINATION
    // ================================================================

    public async Task<PaginatedResult<ModuleDto>> GetModulesByCourseAsync(int courseId, PaginationRequest pagination, bool isInstructor)
    {
        var paginatedModules = await _repository.GetModulesByCourseIdAsync(courseId, pagination.PageNumber, pagination.PageSize);
        var dtos = paginatedModules.Items.Select(m => MapToModuleDto(m, !isInstructor)).ToList();
        return new PaginatedResult<ModuleDto>(dtos, paginatedModules.TotalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ModuleDto> CreateModuleAsync(CreateModuleRequest request)
    {
        var existingModules = await _repository.GetModulesByCourseIdAsync(request.CourseId, 1, 100);

        var module = new CourseModule(request.CourseId, request.Title, request.Description, request.Order);

        // Domain-level validation
        module.ValidateUniqueOrder(existingModules.Items);

        _repository.AddModule(module);
        await _context.SaveChangesAsync();

        return MapToModuleDto(module, false);
    }

    public async Task<ModuleDto?> UpdateModuleAsync(Guid moduleId, UpdateModuleRequest request)
    {
        var module = await _repository.GetModuleByIdAsync(moduleId);
        if (module == null) return null;

        // Get all modules in the same course
        var existingModules = await _repository.GetModulesByCourseIdAsync(module.CourseId, 1, 100);

        // Validate new order doesn't conflict (excludes current module)
        module.ValidateOrder(request.Order, existingModules.Items);

        module.Update(request.Title, request.Description, request.Order);
        _repository.UpdateModule(module);
        await _context.SaveChangesAsync();

        return MapToModuleDto(module, false);
    }

    public async Task<bool> DeleteModuleAsync(Guid moduleId)
    {
        var module = await _repository.GetModuleByIdAsync(moduleId);
        if (module == null) return false;
        _repository.DeleteModule(module);
        await _context.SaveChangesAsync();
        return true;
    }

    // ================================================================
    // LESSON OPERATIONS WITH PAGINATION
    // ================================================================

    public async Task<PaginatedResult<LessonDto>> GetLessonsByModuleAsync(Guid moduleId, PaginationRequest pagination, bool isInstructor)
    {
        var paginatedLessons = await _repository.GetLessonsByModuleIdAsync(moduleId, pagination.PageNumber, pagination.PageSize, !isInstructor);
        var dtos = paginatedLessons.Items.Select(MapToLessonDto).ToList();
        return new PaginatedResult<LessonDto>(dtos, paginatedLessons.TotalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<LessonDto?> GetLessonByIdAsync(Guid lessonId, bool isInstructor)
    {
        var lesson = await _repository.GetLessonByIdAsync(lessonId);
        if (lesson == null) return null;
        if (!isInstructor && !lesson.IsAccessible) return null;
        return MapToLessonDto(lesson);
    }

    public async Task<LessonDto?> CreateLessonAsync(Guid moduleId, CreateLessonRequest request)
    {
        var module = await _repository.GetModuleByIdAsync(moduleId);
        if (module == null) return null;

        try
        {
            var lesson = module.AddLesson(
                request.Title, request.Content, request.VideoUrl,
                request.Order, request.DurationMinutes);

            _repository.AddLesson(lesson);
            await _context.SaveChangesAsync();
            return MapToLessonDto(lesson);
        }
        catch (InvalidOperationException ex)
        {
            // Re-throw so controller can catch it
            throw;
        }
    }

    public async Task<LessonDto?> UpdateLessonAsync(Guid lessonId, UpdateLessonRequest request)
    {
        var lesson = await _repository.GetLessonByIdAsync(lessonId);
        if (lesson == null) return null;

        // Get all lessons in the same module to check for order conflicts
        var existingLessons = await _repository.GetLessonsByModuleIdAsync(
            lesson.ModuleId, 1, 100, publishedOnly: false);

        // Validate the new order doesn't conflict (excludes current lesson)
        lesson.ValidateOrder(request.Order, existingLessons.Items);

        lesson.Update(request.Title, request.Content, request.VideoUrl,
            request.Order, request.DurationMinutes);
        _repository.UpdateLesson(lesson);
        await _context.SaveChangesAsync();
        return MapToLessonDto(lesson);
    }

    public async Task<bool> PublishLessonAsync(Guid lessonId)
    {
        var lesson = await _repository.GetLessonByIdAsync(lessonId);
        if (lesson == null) return false;
        lesson.Publish();
        _repository.UpdateLesson(lesson);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AttachResourceAsync(Guid lessonId, Guid fileId, string resourceType)
    {
        var fileExists = await _fileServiceClient.FileExistsAsync(fileId);
        if (!fileExists) return false;
        var lesson = await _repository.GetLessonByIdAsync(lessonId);
        if (lesson == null) return false;
        lesson.AttachResource(fileId, resourceType);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CourseHasContentAsync(int courseId)
    {
        var modules = await _repository.GetModulesByCourseIdAsync(courseId, 1, 100);
        return modules.Items.Any(m => m.PublishedLessonCount > 0);
    }

    public async Task<LessonDto?> GetLessonInfoAsync(Guid lessonId)
    {
        var lesson = await _repository.GetLessonByIdAsync(lessonId);
        return lesson != null ? MapToLessonDto(lesson) : null;
    }

    private static ModuleDto MapToModuleDto(CourseModule module, bool publishedOnly)
    {
        var lessons = publishedOnly ? module.Lessons.Where(l => l.IsAccessible) : module.Lessons;
        return new ModuleDto
        {
            Id = module.Id,
            CourseId = module.CourseId,
            Title = module.Title,
            Description = module.Description,
            Order = module.Order,
            LessonCount = lessons.Count(),
            TotalDurationMinutes = lessons.Sum(l => l.DurationMinutes),
            CreatedAt = module.CreatedAt,
            UpdatedAt = module.UpdatedAt
        };
    }

    private static LessonDto MapToLessonDto(Lesson lesson) => new()
    {
        Id = lesson.Id,
        ModuleId = lesson.ModuleId,
        Title = lesson.Title,
        Content = lesson.Content,
        VideoUrl = lesson.VideoUrl,
        Order = lesson.Order,
        DurationMinutes = lesson.DurationMinutes,
        Status = lesson.Status.ToString(),
        CreatedAt = lesson.CreatedAt,
        UpdatedAt = lesson.UpdatedAt,
        Resources = lesson.Resources.Select(r => new ResourceDto
        {
            Id = r.Id,
            FileId = r.FileId,
            ResourceType = r.ResourceType,
            AttachedAt = r.AttachedAt
        }).ToList()
    };
}