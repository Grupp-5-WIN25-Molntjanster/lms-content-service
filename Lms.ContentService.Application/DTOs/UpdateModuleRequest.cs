using System.ComponentModel.DataAnnotations;

namespace Lms.ContentService.Application.DTOs;

public class UpdateModuleRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int Order { get; set; }
}