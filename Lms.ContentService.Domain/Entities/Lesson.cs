using Lms.ContentService.Domain.Common;
using Lms.ContentService.Domain.Enums;

namespace Lms.ContentService.Domain.Entities;

/// <summary>
/// ENTITY: Lesson
/// 
/// A single unit of educational content within a module.
/// Has its own identity and lifecycle (Draft → Published → Archived).
/// 
/// WHY NOT AGGREGATE ROOT:
/// - Lessons only exist within a module
/// - No external entity references a lesson directly by ID
/// - Business rules are enforced by the module (aggregate root)
/// </summary>
public class Lesson : BaseEntity
{
    public Guid ModuleId { get; private set; }
    public string Title { get; private set; }
    public string? Content { get; private set; }
    public string? VideoUrl { get; private set; }
    public int Order { get; private set; }
    public int DurationMinutes { get; private set; }
    public LessonStatus Status { get; private set; }

    public CourseModule Module { get; private set; } = null!;
    private readonly List<LessonResource> _resources = new();
    public IReadOnlyCollection<LessonResource> Resources => _resources.AsReadOnly();

    private Lesson() { }

    public Lesson(Guid moduleId, string title, string? content, string? videoUrl, int order, int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Lesson title is required.", nameof(title));

        ModuleId = moduleId;
        Title = title.Trim();
        Content = content?.Trim();
        VideoUrl = videoUrl?.Trim();
        Order = order;
        DurationMinutes = durationMinutes >= 0 ? durationMinutes : 0;
        Status = LessonStatus.Draft;
        SetCreated();
    }

    public void Update(string title, string? content, string? videoUrl, int order, int durationMinutes)
    {
        Title = title.Trim();
        Content = content?.Trim();
        VideoUrl = videoUrl?.Trim();
        Order = order;
        DurationMinutes = durationMinutes;
        SetUpdated();
    }

    public void Publish()
    {
        if (Status == LessonStatus.Archived)
            throw new InvalidOperationException("Archived lessons cannot be published.");
        Status = LessonStatus.Published;
        SetUpdated();
    }

    public void Unpublish()
    {
        Status = LessonStatus.Draft;
        SetUpdated();
    }

    public void Archive()
    {
        Status = LessonStatus.Archived;
        SetUpdated();
    }

    public bool IsAccessible => Status == LessonStatus.Published;

    public void AttachResource(Guid fileId, string resourceType)
    {
        if (_resources.Any(r => r.FileId == fileId))
            throw new InvalidOperationException("File already attached to this lesson.");
        _resources.Add(new LessonResource(Id, fileId, resourceType));
    }

    public void RemoveResource(Guid fileId)
    {
        var resource = _resources.FirstOrDefault(r => r.FileId == fileId);
        if (resource != null) _resources.Remove(resource);
    }

    /// <summary>
    /// Validates that the new order doesn't conflict with existing lessons in the same module.
    /// Excludes the current lesson (for updates).
    /// </summary>
    public void ValidateOrder(int newOrder, IEnumerable<Lesson> existingLessons)
    {
        if (existingLessons.Any(l => l.Order == newOrder && l.Id != Id))
            throw new InvalidOperationException(
                $"A lesson with order {newOrder} already exists in this module.");
    }
}