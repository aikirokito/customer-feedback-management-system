using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", table =>
        {
            table.HasCheckConstraint(
                "CK_notifications_Type_Valid",
                "\"Type\" IN ('FeedbackSubmitted', 'FeedbackAssigned', 'FeedbackStatusChanged', 'FeedbackResponseAdded', 'FeedbackCommentAdded', 'FeedbackResolved', 'FeedbackRejected', 'FeedbackClosed', 'SystemAlert')");
        });
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.RelatedEntityType).HasMaxLength(100);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
