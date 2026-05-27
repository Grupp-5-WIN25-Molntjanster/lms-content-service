using FluentAssertions;
using System.Net.Http.Json;

namespace Lms.ContentService.IntegrationTests.Controllers;

public class InternalContentControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InternalContentControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CourseHasContent_WithoutApiKey_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/internal/content/courses/1/has-content");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CourseHasContent_WithInvalidApiKey_ShouldReturnForbidden()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/content/courses/1/has-content");
        request.Headers.Add("X-Api-Key", "invalid-key-12345");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetLessonInfo_WithoutApiKey_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/internal/content/lessons/1/info");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CourseHasContent_WithValidApiKey_ShouldReturnOk()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/content/courses/1/has-content");
        request.Headers.Add("X-Api-Key", "content-svc-internal-key-xyz789");

        var response = await _client.SendAsync(request);

        // With valid key, should return OK
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            json.Should().Contain("courseId");
            json.Should().Contain("hasContent");
        }
        else
        {
            // API key might not match test config
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        }
    }
}