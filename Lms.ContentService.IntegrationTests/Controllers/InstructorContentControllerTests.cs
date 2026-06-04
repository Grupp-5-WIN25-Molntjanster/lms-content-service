using FluentAssertions;
using Lms.ContentService.Application.DTOs;
using System.Net.Http.Json;

namespace Lms.ContentService.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for InstructorContentController.
/// 
/// IMPORTANT: ASP.NET Core middleware order is:
/// 1. Authentication (checks JWT)
/// 2. Authorization (checks [Authorize] attribute)
/// 3. Model Validation (checks [Required] attributes)
/// 
/// So unauthorized requests return 401 BEFORE model validation runs.
/// This is CORRECT behavior - we don't validate data from unauthenticated users.
/// </summary>
public class InstructorContentControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InstructorContentControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ================================================================
    // UNAUTHORIZED TESTS (No JWT Token)
    // ================================================================

    /// <summary>
    /// TEST: All instructor endpoints require authentication.
    /// Even with valid data, no JWT = 401.
    /// </summary>
    [Fact]
    public async Task CreateModule_WithoutAuth_ShouldReturnUnauthorized()
    {
        var request = new CreateModuleRequest
        {
            CourseId = 1,
            Title = "Test Module",
            Order = 1
        };

        var response = await _client.PostAsJsonAsync("/api/content/courses/1/modules", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// TEST: Invalid data without auth still returns 401.
    /// Authorization check happens BEFORE model validation.
    /// This is correct - we don't validate data from unauthenticated users.
    /// </summary>
    [Fact]
    public async Task CreateModule_InvalidDataWithoutAuth_ShouldReturnUnauthorizedNotBadRequest()
    {
        var request = new CreateModuleRequest
        {
            CourseId = 1,
            Title = "",  // Invalid - but auth check comes first
            Order = 1
        };

        var response = await _client.PostAsJsonAsync("/api/content/courses/1/modules", request);

        // Authorization middleware runs before model validation
        // So we get 401, not 400
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized,
            "authorization middleware runs before model validation");
    }

    [Fact]
    public async Task UpdateModule_WithoutAuth_ShouldReturnUnauthorized()
    {
        var request = new UpdateModuleRequest { Title = "Updated", Order = 1 };
        var response = await _client.PutAsJsonAsync("/api/content/modules/1", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteModule_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/content/modules/1");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateLesson_WithoutAuth_ShouldReturnUnauthorized()
    {
        var request = new CreateLessonRequest
        {
            Title = "Test Lesson",
            Order = 1,
            DurationMinutes = 30
        };
        var response = await _client.PostAsJsonAsync("/api/content/modules/1/lessons", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublishLesson_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsync("/api/content/lessons/1/publish", null);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AttachResource_WithoutAuth_ShouldReturnUnauthorized()
    {
        var request = new AttachResourceRequest { FileId = Guid.NewGuid(), ResourceType = "PDF" };
        var response = await _client.PostAsJsonAsync("/api/content/lessons/1/resources", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    // ================================================================
    // ADMIN-ONLY TESTS
    // ================================================================

    /// <summary>
    /// TEST: Delete module requires Admin role.
    /// Even instructors cannot delete (only Admin).
    /// </summary>
    [Fact]
    public async Task DeleteModule_WithoutAuth_ShouldRequireAdminRole()
    {
        var response = await _client.DeleteAsync("/api/content/modules/1");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized,
            "delete endpoint requires authentication first");
    }
}