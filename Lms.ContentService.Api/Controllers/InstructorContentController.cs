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

    /// <summary>
    /// Create a new module in a course.
    /// Returns 409 Conflict if a module with the same order already exists.
    /// </summary>
    [HttpPost("courses/{courseId:int}/modules")]
    public async Task<IActionResult> CreateModule(int courseId, [FromBody] CreateModuleRequest request)
    {
        request.CourseId = courseId;

        try
        {
            var module = await _contentService.CreateModuleAsync(request);
            return CreatedAtAction(nameof(StudentContentController.GetModules),
                "StudentContent", new { courseId }, module);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("order"))
        {
            // Duplicate order → return 409 Conflict
            return Conflict(new { error = "duplicate_order", message = ex.Message });
        }
    }

    [HttpPut("modules/{moduleId}")]
    public async Task<IActionResult> UpdateModule(Guid moduleId, [FromBody] UpdateModuleRequest request)
    {
        try
        {
            var module = await _contentService.UpdateModuleAsync(moduleId, request);
            return module == null
                ? NotFound(new { error = "module_not_found", message = "Module not found." })
                : Ok(module);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("order"))
        {
            return Conflict(new { error = "duplicate_module_order", message = ex.Message });
        }
    }

    [HttpDelete("modules/{moduleId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteModule(Guid moduleId)
    {
        return await _contentService.DeleteModuleAsync(moduleId) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Create a new lesson in a module.
    /// Returns 409 Conflict if a lesson with the same order already exists.
    /// </summary>
    [HttpPost("modules/{moduleId}/lessons")]
    public async Task<IActionResult> CreateLesson(Guid moduleId, [FromBody] CreateLessonRequest request)
    {
        try
        {
            var lesson = await _contentService.CreateLessonAsync(moduleId, request);
            return lesson == null
                ? NotFound(new { error = "module_not_found", message = "Module not found." })
                : CreatedAtAction(nameof(StudentContentController.GetLesson),
                    "StudentContent", new { lessonId = lesson.Id }, lesson);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("order"))
        {
            return Conflict(new { error = "duplicate_lesson_order", message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing lesson.
    /// Returns 409 Conflict if the new order conflicts with another lesson.
    /// </summary>
    [HttpPut("lessons/{lessonId}")]
    public async Task<IActionResult> UpdateLesson(Guid lessonId, [FromBody] UpdateLessonRequest request)
    {
        try
        {
            var lesson = await _contentService.UpdateLessonAsync(lessonId, request);
            return lesson == null
                ? NotFound(new { error = "lesson_not_found", message = "Lesson not found." })
                : Ok(lesson);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("order"))
        {
            return Conflict(new { error = "duplicate_lesson_order", message = ex.Message });
        }
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