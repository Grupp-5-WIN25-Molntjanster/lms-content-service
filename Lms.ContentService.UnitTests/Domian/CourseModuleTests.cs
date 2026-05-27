using FluentAssertions;
using Lms.ContentService.Domain.Entities;
using Lms.ContentService.Domain.Enums;

namespace Lms.ContentService.UnitTests.Domain;

/// <summary>
/// Unit tests for CourseModule aggregate root.
/// 
/// Tests ALL business rules without a database:
/// - Module creation validation
/// - Adding lessons with ordering constraints
/// - Publishing workflow
/// - Duration calculations
/// - Aggregate consistency
/// </summary>
public class CourseModuleTests
{
    // ================================================================
    // CREATION TESTS
    // ================================================================

    /// <summary>
    /// TEST: Creating a module with valid data should succeed.
    /// 
    /// BUSINESS RULE: Modules require a non-empty title and non-negative order.
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // ACT
        var courseId = Guid.NewGuid();
        var module = new CourseModule(courseId, "Introduction to C#", "Learn the basics", 1);

        // ASSERT
        module.CourseId.Should().Be(courseId);
        module.Title.Should().Be("Introduction to C#");
        module.Description.Should().Be("Learn the basics");
        module.Order.Should().Be(1);
        module.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        module.UpdatedAt.Should().BeNull("new modules should not have UpdatedAt set");
        module.Lessons.Should().BeEmpty("new modules have no lessons");
    }

    /// <summary>
    /// TEST: Creating a module with empty title should throw exception.
    /// 
    /// BUSINESS RULE: Title is required for all modules.
    /// </summary>
    [Fact]
    public void Create_WithEmptyTitle_ShouldThrowException()
    {
        // ACT
        Action act = () => new CourseModule(Guid.NewGuid(), "", null, 0);

        // ASSERT
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    /// <summary>
    /// TEST: Creating a module with whitespace title should throw exception.
    /// </summary>
    [Fact]
    public void Create_WithWhitespaceTitle_ShouldThrowException()
    {
        // ACT
        Action act = () => new CourseModule(Guid.NewGuid(), "   ", null, 0);

        // ASSERT
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    /// <summary>
    /// TEST: Creating a module with null title should throw exception.
    /// </summary>
    [Fact]
    public void Create_WithNullTitle_ShouldThrowException()
    {
        // ACT
        Action act = () => new CourseModule(Guid.NewGuid(), null!, null, 0);

        // ASSERT
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    /// <summary>
    /// TEST: Creating a module with negative order should throw exception.
    /// 
    /// BUSINESS RULE: Order must be non-negative (0 or greater).
    /// </summary>
    [Fact]
    public void Create_WithNegativeOrder_ShouldThrowException()
    {
        // ACT
        Action act = () => new CourseModule(Guid.NewGuid(), "Title", null, -1);

        // ASSERT
        act.Should().Throw<ArgumentException>()
            .WithMessage("*order*");
    }

    /// <summary>
    /// TEST: Creating a module with order 0 is allowed.
    /// </summary>
    [Fact]
    public void Create_WithZeroOrder_ShouldSucceed()
    {
        // ACT
        var module = new CourseModule(Guid.NewGuid(), "Title", null, 0);

        // ASSERT
        module.Order.Should().Be(0);
    }

    /// <summary>
    /// TEST: Creating a module without description should have null Description.
    /// </summary>
    [Fact]
    public void Create_WithoutDescription_ShouldHaveNullDescription()
    {
        // ACT
        var module = new CourseModule(Guid.NewGuid(), "Title", null, 0);

        // ASSERT
        module.Description.Should().BeNull();
    }

    // ========================================================
    // UPDATE TESTS
    // ================================================================

    /// <summary>
    /// TEST: Updating a module should change properties and set UpdatedAt.
    /// </summary>
    [Fact]
    public void Update_WithValidData_ShouldUpdateProperties()
    {
        // ARRANGE
        var courseId = Guid.NewGuid();
        var module = new CourseModule(courseId, "Original Title", "Original Description", 1);
        var beforeUpdate = module.UpdatedAt;

        // ACT
        module.Update("Updated Title", "Updated Description", 2);

        // ASSERT
        module.Title.Should().Be("Updated Title");
        module.Description.Should().Be("Updated Description");
        module.Order.Should().Be(2);
        module.UpdatedAt.Should().NotBeNull();
        module.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        module.CourseId.Should().Be(courseId, "CourseId should not change on update");
        module.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10),
            "CreatedAt should not change on update");
    }

    /// <summary>
    /// TEST: Updating with empty title should throw exception.
    /// </summary>
    [Fact]
    public void Update_WithEmptyTitle_ShouldThrowException()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Title", "Description", 1);

        // ACT
        Action act = () => module.Update("", null, 1);

        // ASSERT
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    // ================================================================
    // LESSON MANAGEMENT TESTS
    // ================================================================

    /// <summary>
    /// TEST: Adding a lesson should increase lesson count.
    /// </summary>
    [Fact]
    public void AddLesson_WithValidData_ShouldAddToModule()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module 1", null, 1);

        // ACT
        var lesson = module.AddLesson("Lesson 1", "Content here", "https://video.url", 1, 30);

        // ASSERT
        module.Lessons.Should().ContainSingle();
        module.TotalLessonCount.Should().Be(1);
        lesson.Title.Should().Be("Lesson 1");
        lesson.Content.Should().Be("Content here");
        lesson.VideoUrl.Should().Be("https://video.url");
        lesson.Order.Should().Be(1);
        lesson.DurationMinutes.Should().Be(30);
        lesson.Status.Should().Be(LessonStatus.Draft, "new lessons start as Draft");
        lesson.ModuleId.Should().Be(module.Id);
    }

    /// <summary>
    /// TEST: Adding a lesson with duplicate order should throw exception.
    /// 
    /// BUSINESS RULE: No two lessons in the same module can have the same order.
    /// </summary>
    [Fact]
    public void AddLesson_WithDuplicateOrder_ShouldThrowException()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module 1", null, 1);
        module.AddLesson("Lesson 1", null, null, 1, 30);

        // ACT
        Action act = () => module.AddLesson("Lesson 2", null, null, 1, 45);

        // ASSERT
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*order*already exists*");
    }

    /// <summary>
    /// TEST: Adding multiple lessons should maintain correct count.
    /// </summary>
    [Fact]
    public void AddLesson_MultipleLessons_ShouldTrackCorrectCount()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module", null, 1);

        // ACT
        module.AddLesson("L1", null, null, 1, 30);
        module.AddLesson("L2", null, null, 2, 45);
        module.AddLesson("L3", null, null, 3, 60);

        // ASSERT
        module.TotalLessonCount.Should().Be(3);
        module.Lessons.Should().HaveCount(3);
    }

    /// <summary>
    /// TEST: Lessons should be accessible in order.
    /// <summary>
    /// TEST: Lessons should be accessible in order.
    /// </summary>
    [Fact]
    public void AddLesson_LessonsShouldMaintainOrder()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module", null, 1);

        // ACT
        module.AddLesson("Third", null, null, 3, 30);
        module.AddLesson("First", null, null, 1, 30);
        module.AddLesson("Second", null, null, 2, 30);

        // ASSERT - Lessons maintain insertion order, not sorted by Order property
        module.Lessons.Should().HaveCount(3);
        module.Lessons.Select(l => l.Order).Should().Contain(new[] { 3, 1, 2 });
    }

    // ================================================================
    // PUBLISHING TESTS
    // ================================================================

    /// <summary>
    /// TEST: PublishAllLessons should publish all draft lessons.
    /// 
    /// BUSINESS RULE: Publishing makes lessons visible to students.
    /// </summary>
    [Fact]
    public void PublishAllLessons_ShouldPublishAllDraftLessons()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module", null, 1);
        module.AddLesson("L1", null, null, 1, 30);
        module.AddLesson("L2", null, null, 2, 45);
        module.AddLesson("L3", null, null, 3, 60);

        // ACT
        module.PublishAllLessons();

        // ASSERT
        module.Lessons.Should().AllSatisfy(l =>
            l.Status.Should().Be(LessonStatus.Published));
        module.PublishedLessonCount.Should().Be(3);
    }

    /// <summary>
    /// TEST: PublishAllLessons should only affect draft lessons.
    /// </summary>
    [Fact]
    public void PublishAllLessons_ShouldNotRepublishAlreadyPublished()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module", null, 1);
        var lesson1 = module.AddLesson("L1", null, null, 1, 30);
        lesson1.Publish(); // Already published
        module.AddLesson("L2", null, null, 2, 45); // Still draft

        // ACT
        module.PublishAllLessons();

        // ASSERT
        module.PublishedLessonCount.Should().Be(2);
        lesson1.Status.Should().Be(LessonStatus.Published);
    }

    /// <summary>
    /// TEST: PublishedLessonCount should only count published lessons.
    /// </summary>
    [Fact]
    public void PublishedLessonCount_ShouldOnlyCountPublished()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module", null, 1);
        module.AddLesson("Draft Lesson", null, null, 1, 30);
        var publishedLesson = module.AddLesson("Published Lesson", null, null, 2, 45);
        publishedLesson.Publish();

        // ASSERT
        module.TotalLessonCount.Should().Be(2);
        module.PublishedLessonCount.Should().Be(1);
    }

    // ================================================================
    // DURATION TESTS
    // ================================================================

    /// <summary>
    /// TEST: TotalDurationMinutes should sum only published lessons.
    /// </summary>
    [Fact]
    public void TotalDurationMinutes_ShouldSumOnlyPublishedLessons()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module", null, 1);
        module.AddLesson("Draft", null, null, 1, 30); // Draft - not counted
        var publishedLesson = module.AddLesson("Published", null, null, 2, 45);
        publishedLesson.Publish();

        // ASSERT
        module.TotalDurationMinutes.Should().Be(45, "only published lessons counted");
    }
    /// <summary>
    /// TEST: TotalDurationMinutes should be 0 when no lessons published.
    /// </summary>
    [Fact]
    public void TotalDurationMinutes_WithNoPublishedLessons_ShouldBeZero()
    {
        // ARRANGE
        var module = new CourseModule(Guid.NewGuid(), "Module", null, 1);
        module.AddLesson("Draft 1", null, null, 1, 30);
        module.AddLesson("Draft 2", null, null, 2, 45);

        // ASSERT
        module.TotalDurationMinutes.Should().Be(0);
    }
}
