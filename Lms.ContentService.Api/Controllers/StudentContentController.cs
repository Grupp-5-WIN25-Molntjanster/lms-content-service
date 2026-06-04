using Lms.ContentService.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.ContentService.Api.Controllers;

[ApiController]
[Route("api/content")]
public class StudentContentController : ControllerBase
{
    private readonly Lms.ContentService.Application.Services.ContentService _contentService;
    public StudentContentController(Lms.ContentService.Application.Services.ContentService contentService) => _contentService = contentService;

    [HttpGet("courses/{courseId:int}/modules")]
    [AllowAnonymous]
    public async Task<IActionResult> GetModules(int courseId, [FromQuery] PaginationRequest pagination)
    {
        var isInstructor = User.IsInRole("Instructor") || User.IsInRole("Admin");
        var result = await _contentService.GetModulesByCourseAsync(courseId, pagination, isInstructor);
        return Ok(result);
    }

    [HttpGet("modules/{moduleId}/lessons")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLessons(Guid moduleId, [FromQuery] PaginationRequest pagination)
    {
        var isInstructor = User.IsInRole("Instructor") || User.IsInRole("Admin");
        var result = await _contentService.GetLessonsByModuleAsync(moduleId, pagination, isInstructor);
        return Ok(result);
    }

    [HttpGet("lessons/{lessonId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLesson(Guid lessonId)
    {
        var isInstructor = User.IsInRole("Instructor") || User.IsInRole("Admin");
        var lesson = await _contentService.GetLessonByIdAsync(lessonId, isInstructor);
        return lesson == null ? NotFound() : Ok(lesson);
    }
}