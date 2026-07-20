using CFMS.Domain.Entities;
using CFMS.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using FeedbackEntity = CFMS.Domain.Entities.Feedback;

namespace CFMS.Tests.Persistence;

public class AppDbContextModelTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=cfms_model_tests;Username=postgres;Password=unused")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void AssignmentModel_HasNoShadowUserForeignKey_AndOnlyOneActiveAssignmentIndex()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(FeedbackAssignment))!;

        entity.FindProperty("UserId").Should().BeNull();

        var feedbackId = entity.FindProperty(nameof(FeedbackAssignment.FeedbackId))!;
        var index = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Count == 1 && candidate.Properties[0] == feedbackId);

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("\"IsActive\" = TRUE");
    }

    [Fact]
    public void FeedbackModel_RequiresCategory_AndConstrainsRatingAndEnums()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(FeedbackEntity))!;

        entity.FindProperty(nameof(FeedbackEntity.CategoryId))!.IsNullable.Should().BeFalse();
        entity.GetCheckConstraints().Select(constraint => constraint.Name).Should().Contain(new[]
        {
            "CK_feedbacks_Rating_Range",
            "CK_feedbacks_Status_Valid",
            "CK_feedbacks_Priority_Valid"
        });
    }
}
