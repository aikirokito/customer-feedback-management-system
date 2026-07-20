using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class FeedbackCategoryEntityConfiguration : IEntityTypeConfiguration<FeedbackCategoryEntity>
{
    public void Configure(EntityTypeBuilder<FeedbackCategoryEntity> builder)
    {
        builder.ToTable("feedback_categories");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);
            
        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasOne(c => c.Department)
            .WithMany(d => d.Categories)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
