using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class FeedbackAttachmentConfiguration : IEntityTypeConfiguration<FeedbackAttachment>
{
    public void Configure(EntityTypeBuilder<FeedbackAttachment> builder)
    {
        builder.ToTable("feedback_attachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(256);
        builder.Property(a => a.StorageKey).IsRequired().HasMaxLength(1024);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(128);
        builder.HasIndex(a => a.FeedbackId);

        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
