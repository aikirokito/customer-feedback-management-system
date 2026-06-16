using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class FeedbackStatusHistoryConfiguration : IEntityTypeConfiguration<FeedbackStatusHistory>
{
    public void Configure(EntityTypeBuilder<FeedbackStatusHistory> builder)
    {
        builder.ToTable("feedback_status_history");
        builder.HasKey(sh => sh.Id);
        builder.Property(sh => sh.FromStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(sh => sh.ToStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(sh => sh.Reason).HasMaxLength(500);
        builder.HasIndex(sh => sh.FeedbackId);

        builder.HasOne(sh => sh.ChangedByUser)
            .WithMany()
            .HasForeignKey(sh => sh.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
