using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedbacks", table =>
        {
            table.HasCheckConstraint(
                "CK_feedbacks_Rating_Range",
                "[Rating] IS NULL OR [Rating] BETWEEN 1 AND 5");
            table.HasCheckConstraint(
                "CK_feedbacks_Status_Valid",
                "[Status] IN ('Submitted', 'Assigned', 'InProgress', 'Resolved', 'Closed', 'Cancelled')");
            table.HasCheckConstraint(
                "CK_feedbacks_Priority_Valid",
                "[Priority] IN ('Low', 'Medium', 'High', 'Urgent')");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Title).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Description).IsRequired().HasMaxLength(5000);

        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(f => f.Priority).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.CategoryId);
        builder.HasIndex(f => f.DepartmentId);
        builder.HasIndex(f => f.Priority);
        builder.HasIndex(f => f.SubmittedByUserId);
        builder.HasIndex(f => f.AssignedToUserId);
        builder.HasIndex(f => f.CreatedAtUtc);

        builder.HasOne(f => f.AssignedToUser)
            .WithMany()
            .HasForeignKey(f => f.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(f => f.Category)
            .WithMany(c => c.Feedbacks)
            .HasForeignKey(f => f.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Department)
            .WithMany(d => d.Feedbacks)
            .HasForeignKey(f => f.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(f => f.Attachments)
            .WithOne(a => a.Feedback)
            .HasForeignKey(a => a.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Responses)
            .WithOne(r => r.Feedback)
            .HasForeignKey(r => r.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Comments)
            .WithOne(c => c.Feedback)
            .HasForeignKey(c => c.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.StatusHistory)
            .WithOne(sh => sh.Feedback)
            .HasForeignKey(sh => sh.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.AssignmentHistory)
            .WithOne(a => a.Feedback)
            .HasForeignKey(a => a.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
