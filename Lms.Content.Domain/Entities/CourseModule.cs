using Lms.ContentService.Domain.Common;

namespace Lms.ContentService.Domain.Entities;

/// <summary>
/// AGGREGATE ROOT: CourseModule
/// 
/// Represents a module (section) within a course.
/// Contains lessons and enforces business rules.
/// 
/// DDD AGGREGATE PATTERN:
/// - CourseModule is the aggregate root
/// - All changes to lessons go THROUGH the module
/// - Repository only exists for CourseModule
/// - Cascade delete: delete module → deletes all lessons + resources
/// </summary>
public class CourseModule : BaseEntity
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public int Order { get; private set; }

    private readonly List<Lesson> _lessons = new();
    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    // EF Core constructor
    private CourseModule() { }

    public CourseModule(Guid courseId, string title, string? description, int order)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Module title is required.", nameof(title));
        if (order < 0)
            throw new ArgumentException("Order must be non-negative.", nameof(order));

        CourseId = courseId;
        Title = title.Trim();
        Description = description?.Trim();
        Order = order;
        SetCreated();
    }

    public void Update(string title, string? description, int order)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Module title is required.", nameof(title));

        Title = title.Trim();
        Description = description?.Trim();
        Order = order;
        SetUpdated();
    }

    public Lesson AddLesson(string title, string? content, string? videoUrl, int order, int durationMinutes)
    {
        if (_lessons.Any(l => l.Order == order))
            throw new InvalidOperationException($"Lesson with order {order} already exists in this module.");

        var lesson = new Lesson(Id, title, content, videoUrl, order, durationMinutes);
        _lessons.Add(lesson);
        return lesson;
    }

    public void PublishAllLessons()
    {
        foreach (var lesson in _lessons.Where(l => l.Status == Enums.LessonStatus.Draft))
            lesson.Publish();
        SetUpdated();
    }

    public int TotalDurationMinutes => _lessons
        .Where(l => l.Status == Enums.LessonStatus.Published)
        .Sum(l => l.DurationMinutes);

    public int PublishedLessonCount => _lessons.Count(l => l.Status == Enums.LessonStatus.Published);
    public int TotalLessonCount => _lessons.Count;
}