using Lms.ContentService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lms.ContentService.Api.Controllers;

[ApiController]
[Route("api/internal/content")]
public class InternalContentController : ControllerBase
{
    private readonly Lms.ContentService.Application.Services.ContentService _contentService;
    public InternalContentController(Lms.ContentService.Application.Services.ContentService contentService) => _contentService = contentService;

    [HttpGet("courses/{courseId:int}/has-content")]
    public async Task<IActionResult> CourseHasContent(int courseId)
    {
        var hasContent = await _contentService.CourseHasContentAsync(courseId);
        return Ok(new { courseId, hasContent });
    }

    [HttpGet("lessons/{lessonId}/info")]
    public async Task<IActionResult> GetLessonInfo(Guid lessonId)
    {
        var lesson = await _contentService.GetLessonInfoAsync(lessonId);
        return lesson == null ? NotFound() : Ok(lesson);
    }
}