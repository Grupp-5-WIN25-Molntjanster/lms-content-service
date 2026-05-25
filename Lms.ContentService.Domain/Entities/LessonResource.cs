using Lms.ContentService.Domain.Common;

namespace Lms.ContentService.Domain.Entities;

/// <summary>
/// VALUE OBJECT / ENTITY: LessonResource
/// 
/// Represents a file attached to a lesson.
/// FileId is a reference to File Service (external microservice).
/// 
/// WHY BOTH:
/// - Has its own Id (entity characteristic)
/// - Has no independent existence outside Lesson (value object characteristic)
/// - Treated as part of the Lesson aggregate
/// </summary>
public class LessonResource : BaseEntity
{
    public Guid LessonId { get; private set; }
    public Guid FileId { get; private set; }
    public string ResourceType { get; private set; }
    public DateTime AttachedAt { get; private set; }

    public Lesson Lesson { get; private set; } = null!;

    private LessonResource() { }

    public LessonResource(Guid lessonId, Guid fileId, string resourceType)
    {
        LessonId = lessonId;
        FileId = fileId;
        ResourceType = resourceType;
        AttachedAt = DateTime.UtcNow;
        SetCreated();
    }
}