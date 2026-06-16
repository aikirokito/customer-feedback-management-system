using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class FeedbackResponseConfiguration : IEntityTypeConfiguration<FeedbackResponse>
{
    public void Configure(EntityTypeBuilder<FeedbackResponse> builder)
    {
        builder.ToTable("feedback_responses");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Content).IsRequired().HasMaxLength(5000);
        builder.HasIndex(r => r.FeedbackId);

        builder.HasOne(r => r.RespondedByUser)
            .WithMany(u => u.Responses)
            .HasForeignKey(r => r.RespondedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
