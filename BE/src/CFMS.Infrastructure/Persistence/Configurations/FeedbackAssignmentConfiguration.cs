using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class FeedbackAssignmentConfiguration : IEntityTypeConfiguration<FeedbackAssignment>
{
    public void Configure(EntityTypeBuilder<FeedbackAssignment> builder)
    {
        builder.ToTable("feedback_assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Note).HasMaxLength(1000);
        builder.HasIndex(a => a.FeedbackId)
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE");
        builder.HasIndex(a => a.AssignedToUserId);

        builder.HasOne(a => a.AssignedToUser)
            .WithMany(u => u.Assignments)
            .HasForeignKey(a => a.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedByUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
