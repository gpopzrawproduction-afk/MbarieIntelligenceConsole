using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the User entity.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasColumnType("varchar(256)");

		// UPDATED: Salt length for Argon2 hashes
		builder.Property(u => u.Salt)
			.IsRequired()
			.HasMaxLength(256)
			.HasColumnType("varchar(256)");

		// NEW: FullName replaces DisplayName
		builder.Property(u => u.FullName)
			.HasMaxLength(100)
			.HasColumnType("varchar(100)");

		builder.Property(u => u.JobPosition)
			.HasMaxLength(100)
			.HasColumnType("varchar(100)");

		builder.Property(u => u.Department)
			.HasMaxLength(100)
			.HasColumnType("varchar(100)");

		builder.Property(u => u.SeniorityLevel)
			.HasMaxLength(50)
			.HasColumnType("varchar(50)");

		// NEW: Role is required
		builder.Property(u => u.Role)
			.IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasColumnType("boolean");

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
    }
}