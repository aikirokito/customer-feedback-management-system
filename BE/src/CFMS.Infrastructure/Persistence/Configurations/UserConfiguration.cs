using CFMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFMS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);

        builder.Property(u => u.GoogleSubject).HasMaxLength(256);
        // Partial unique index on GoogleSubject (only where not null) — handled via migration SQL
        builder.HasIndex(u => u.GoogleSubject)
            .HasFilter("\"google_subject\" IS NOT NULL")
            .IsUnique();

        // Role stored as string for readability in the DB
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Status stored as string for readability (not bit flag)
        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.UserStatus.Active);

        // IsActive is a computed property — not mapped to a column
        builder.Ignore(u => u.IsActive);

        // FullName is computed — not mapped to a column
        builder.Ignore(u => u.FullName);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SubmittedFeedbacks)
            .WithOne(f => f.SubmittedByUser)
            .HasForeignKey(f => f.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
