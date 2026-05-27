using FluentAssertions;
using Lms.ContentService.Domain.Entities;
using Lms.ContentService.Domain.Enums;

namespace Lms.ContentService.UnitTests.Domain;

/// <summary>
/// Unit tests for Lesson entity.
/// 
/// Tests:
/// - Lesson creation validation
/// - Publishing workflow (Draft → Published → Archived)
/// - Resource attachment
/// - Business rule enforcement
/// </summary>
public class LessonTests
{
    // ================================================================
    // CREATION TESTS
    // ================================================================

    /// <summary>
    /// TEST: Creating a lesson with valid data should succeed.
    /// New lessons always start as Draft.
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // ACT
        var moduleId = Guid.NewGuid();
        var lesson = new Lesson(moduleId, "Introduction", "Content", null, 1, 30);

        // ASSERT
        lesson.ModuleId.Should().Be(moduleId);
        lesson.Title.Should().Be("Introduction");
        lesson.Content.Should().Be("Content");
        lesson.Order.Should().Be(1);
        lesson.DurationMinutes.Should().Be(30);
        lesson.Status.Should().Be(LessonStatus.Draft, "new lessons start as Draft");
        lesson.IsAccessible.Should().BeFalse("draft lessons are not accessible to students");
        lesson.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        lesson.UpdatedAt.Should().BeNull();
    }

    /// <summary>
    /// TEST: Creating a lesson with empty title should throw exception.
    /// </summary>
    [Fact]
    public void Create_WithEmptyTitle_ShouldThrowException()
    {
        Action act = () => new Lesson(Guid.NewGuid(), "", null, null, 1, 30);
        act.Should().Throw<ArgumentException>().WithMessage("*title*");
    }

    /// <summary>
    /// TEST: Creating a lesson with negative duration should set to 0.
    /// </summary>
    [Fact]
    public void Create_WithNegativeDuration_ShouldSetToZero()
    {
        var lesson = new Lesson(Guid.NewGuid(), "Title", null, null, 1, -10);
        lesson.DurationMinutes.Should().Be(0);
    }

    // ================================================================
    // UPDATE TESTS
    // ================================================================

    /// <summary>
    /// TEST: Updating a lesson should change properties and set UpdatedAt.
    /// </summary>
    [Fact]
    public void Update_ShouldChangeProperties()
    {
        // ARRANGE
        var lesson = new Lesson(Guid.NewGuid(), "Old Title", "Old Content", null, 1, 30);

        // ACT
        lesson.Update("New Title", "New Content", "https://video.url", 2, 45);

        // ASSERT
        lesson.Title.Should().Be("New Title");
        lesson.Content.Should().Be("New Content");
        lesson.VideoUrl.Should().Be("https://video.url");
        lesson.Order.Should().Be(2);
        lesson.DurationMinutes.Should().Be(45);
        lesson.UpdatedAt.Should().NotBeNull();
        lesson.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ================================================================
    // PUBLISHING WORKFLOW TESTS
    // ================================================================

    /// <summary>
    /// TEST: Publishing a draft lesson should change status to Published.
    /// </summary>
    [Fact]
    public void Publish_FromDraft_ShouldBecomePublished()
    {
        // ARRANGE
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);

        // ACT
        lesson.Publish();

        // ASSERT
        lesson.Status.Should().Be(LessonStatus.Published);
        lesson.IsAccessible.Should().BeTrue("published lessons are accessible");
        lesson.UpdatedAt.Should().NotBeNull();
    }

 
        /// <summary>
/// TEST: Unpublishing a published lesson should return to Draft.
/// </summary>
[Fact]
    public void Unpublish_FromPublished_ShouldBecomeDraft()
    {
        // ARRANGE
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);
        lesson.Publish();

        // ACT
        lesson.Unpublish();

        // ASSERT
        lesson.Status.Should().Be(LessonStatus.Draft);
        lesson.IsAccessible.Should().BeFalse();
    }

    /// <summary>
    /// TEST: Archiving a lesson should set status to Archived.
    /// </summary>
    [Fact]
    public void Archive_ShouldSetStatusToArchived()
    {
        // ARRANGE
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);

        // ACT
        lesson.Archive();

        // ASSERT
        lesson.Status.Should().Be(LessonStatus.Archived);
        lesson.IsAccessible.Should().BeFalse("archived lessons are not accessible");
    }

    /// <summary>
    /// TEST: Publishing an archived lesson should throw exception.
    /// 
    /// BUSINESS RULE: Archived lessons cannot be published directly.
    /// They must be recreated or unarchived first.
    /// </summary>
    [Fact]
    public void Publish_FromArchived_ShouldThrowException()
    {
        // ARRANGE
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);
        lesson.Archive();

        // ACT
        Action act = () => lesson.Publish();

        // ASSERT
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Archived*");
    }

    // ================================================================
    // RESOURCE TESTS
    // ================================================================

    /// <summary>
    /// TEST: Attaching a resource should add to lesson.
    /// </summary>
    [Fact]
    public void AttachResource_ShouldAddResource()
    {
        // ARRANGE
        var fileId = Guid.NewGuid();
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);

        // ACT
        lesson.AttachResource(fileId, "PDF");

        // ASSERT
        lesson.Resources.Should().ContainSingle();
        lesson.Resources.First().FileId.Should().Be(fileId);
        lesson.Resources.First().ResourceType.Should().Be("PDF");
        lesson.Resources.First().AttachedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// TEST: Attaching the same file twice should throw exception.
    /// 
    /// BUSINESS RULE: A file can only be attached once to a lesson.
    /// </summary>
    [Fact]
    public void AttachResource_DuplicateFile_ShouldThrowException()
    {
        // ARRANGE
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);
        var fileId = Guid.NewGuid();
        lesson.AttachResource(fileId, "PDF");

        // ACT
        Action act = () => lesson.AttachResource(fileId, "Document");

        // ASSERT
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already attached*");
    }

    /// <summary>
    /// TEST: Removing a resource should remove it from lesson.
    /// </summary>
    [Fact]
    public void RemoveResource_ShouldRemoveResource()
    {
        // ARRANGE
        var moduleId = Guid.NewGuid();
        var fileId1 = Guid.NewGuid();
        var fileId2 = Guid.NewGuid();
        var lesson = new Lesson(moduleId, "Lesson", null, null, 1, 30);
        lesson.AttachResource(fileId1, "PDF");
        lesson.AttachResource(fileId2, "Video");

        // ACT
        lesson.RemoveResource(fileId1);

        // ASSERT
        lesson.Resources.Should().ContainSingle();
        lesson.Resources.First().FileId.Should().Be(fileId2);
    }

    /// <summary>
    /// TEST: Removing a non-existent resource should not throw.
    /// </summary>
    [Fact]
    public void RemoveResource_NonExistent_ShouldNotThrow()
    {
        // ARRANGE
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);

        // ACT
        Action act = () => lesson.RemoveResource(Guid.NewGuid());

        // ASSERT
        act.Should().NotThrow();
    }

    /// <summary>
    /// TEST: Attaching multiple resources should track all of them.
    /// </summary>
    [Fact]
    public void AttachResource_MultipleResources_ShouldTrackAll()
    {
        // ARRANGE
        var fileId1 = Guid.NewGuid();
        var fileId2 = Guid.NewGuid();
        var fileId3 = Guid.NewGuid();
        var lesson = new Lesson(Guid.NewGuid(), "Lesson", null, null, 1, 30);

        // ACT
        lesson.AttachResource(fileId1, "PDF");
        lesson.AttachResource(fileId2, "Video");
        lesson.AttachResource(fileId3, "Quiz");

        // ASSERT
        lesson.Resources.Should().HaveCount(3);
        lesson.Resources.Select(r => r.FileId).Should().Contain(new[] { fileId1, fileId2, fileId3 });
    }
}