using Lms.ContentService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.ContentService.Api.Controllers;

[ApiController]
[Route("api/content")]
[Authorize(Roles = "Instructor,Admin")]
public class InstructorContentController : ControllerBase
{
    private readonly Lms.ContentService.Application.Services.ContentService _contentService;
    public InstructorContentController(Lms.ContentService.Application.Services.ContentService contentService) => _contentService = contentService;

    [HttpPost("courses/{courseId}/modules")]
    public async Task<IActionResult> CreateModule(Guid courseId, [FromBody] CreateModuleRequest request)
    {
        request.CourseId = courseId;
        var module = await _contentService.CreateModuleAsync(request);
        return CreatedAtAction(nameof(StudentContentController.GetModules), "StudentContent", new { courseId }, module);
    }

    [HttpPut("modules/{moduleId}")]
    public async Task<IActionResult> UpdateModule(Guid moduleId, [FromBody] UpdateModuleRequest request)
    {
        var module = await _contentService.UpdateModuleAsync(moduleId, request);
        return module == null ? NotFound() : Ok(module);
    }

    [HttpDelete("modules/{moduleId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteModule(Guid moduleId)
    {
        return await _contentService.DeleteModuleAsync(moduleId) ? NoContent() : NotFound();
    }

    [HttpPost("modules/{moduleId}/lessons")]
    public async Task<IActionResult> CreateLesson(Guid moduleId, [FromBody] CreateLessonRequest request)
    {
        var lesson = await _contentService.CreateLessonAsync(moduleId, request);
        return lesson == null ? NotFound("Module not found.") : CreatedAtAction(nameof(StudentContentController.GetLesson), "StudentContent", new { lessonId = lesson.Id }, lesson);
    }

    [HttpPut("lessons/{lessonId}")]
    public async Task<IActionResult> UpdateLesson(Guid lessonId, [FromBody] UpdateLessonRequest request)
    {
        var lesson = await _contentService.UpdateLessonAsync(lessonId, request);
        return lesson == null ? NotFound() : Ok(lesson);
    }

    [HttpPost("lessons/{lessonId}/publish")]
    public async Task<IActionResult> PublishLesson(Guid lessonId)
    {
        return await _contentService.PublishLessonAsync(lessonId) ? Ok(new { message = "Lesson published." }) : NotFound();
    }

    [HttpPost("lessons/{lessonId}/resources")]
    public async Task<IActionResult> AttachResource(Guid lessonId, [FromBody] AttachResourceRequest request)
    {
        return await _contentService.AttachResourceAsync(lessonId, request.FileId, request.ResourceType) ? Ok(new { message = "Resource attached." }) : BadRequest("File or lesson not found.");
    }
}