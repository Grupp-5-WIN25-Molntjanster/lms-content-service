using FluentAssertions;
using Lms.ContentService.Application.DTOs;
using Lms.ContentService.Application.Common;
using System.Net.Http.Json;

namespace Lms.ContentService.IntegrationTests.Controllers;

public class StudentContentControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StudentContentControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetModules_WithNoModules_ShouldReturnEmptyList()
    {
        var response = await _client.GetAsync(
            "/api/content/courses/00000000-0000-0000-0000-000000000001/modules?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ModuleDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetModules_ShouldSupportPagination()
    {
        var response = await _client.GetAsync(
            "/api/content/courses/00000000-0000-0000-0000-000000000001/modules?pageNumber=1&pageSize=2");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ModuleDto>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetModules_WithInvalidPagination_ShouldUseDefaults()
    {
        var response = await _client.GetAsync(
            "/api/content/courses/00000000-0000-0000-0000-000000000001/modules?pageNumber=-1&pageSize=0");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ModuleDto>>();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetLessons_WithNoLessons_ShouldReturnEmptyList()
    {
        var response = await _client.GetAsync(
            "/api/content/modules/00000000-0000-0000-0000-000000000002/lessons?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<LessonDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLesson_NonExistent_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/api/content/lessons/00000000-0000-0000-0000-00000000FFFF");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetModules_WithoutAuth_ShouldBeAccessible()
    {
        var response = await _client.GetAsync("/api/content/courses/00000000-0000-0000-0000-000000000001/modules");
        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized);
    }
}