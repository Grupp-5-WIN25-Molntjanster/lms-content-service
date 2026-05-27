using FluentAssertions;

namespace Lms.ContentService.IntegrationTests.Middleware;

public class ApiKeyMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiKeyMiddlewareTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InternalEndpoint_NoApiKey_ShouldReturnJsonError()
    {
        var response = await _client.GetAsync("/api/internal/content/courses/1/has-content");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("api_key_missing");
    }

    [Fact]
    public async Task PublicEndpoint_NoApiKey_ShouldNotBeBlocked()
    {
        var response = await _client.GetAsync("/api/content/courses/1/modules");
        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InternalEndpoint_PostMethod_ShouldStillRequireApiKey()
    {
        var response = await _client.PostAsync("/api/internal/content/courses/1/has-content", null);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/internal/content/courses/1/has-content")]
    [InlineData("/api/internal/content/lessons/1/info")]
    public async Task InternalEndpoints_AllRequireApiKey(string path)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}