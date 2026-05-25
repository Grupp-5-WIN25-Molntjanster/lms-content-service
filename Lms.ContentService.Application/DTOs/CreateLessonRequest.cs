using System.ComponentModel.DataAnnotations;

namespace Lms.ContentService.Application.DTOs;

public class CreateLessonRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }
    public string? VideoUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int Order { get; set; }

    [Range(0, int.MaxValue)]
    public int DurationMinutes { get; set; }
}