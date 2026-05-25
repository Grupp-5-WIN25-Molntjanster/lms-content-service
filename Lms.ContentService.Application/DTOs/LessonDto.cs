namespace Lms.ContentService.Application.DTOs;

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
    public int Order { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ResourceDto> Resources { get; set; } = new();
}

public class ResourceDto
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public DateTime AttachedAt { get; set; }
}