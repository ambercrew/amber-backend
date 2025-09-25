using Brainy.Domain.Users.Entities;
using Brainy.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brainy.Infrastructure.Database.EntityConfigurations;

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).IsRequired();

        builder.OwnsOne(
            u => u.Username,
            usernameBuilder =>
            {
                usernameBuilder
                    .Property(u => u.Value)
                    .HasColumnName("Username")
                    .HasMaxLength(30)
                    .IsRequired();

                usernameBuilder
                    .HasIndex(u => u.Value)
                    .HasDatabaseName("users_username_index")
                    .IsUnique();
            }
        );

        builder.OwnsOne(
            u => u.Email,
            emailBuilder =>
            {
                emailBuilder
                    .Property(u => u.Value)
                    .HasColumnName("Email")
                    .HasMaxLength(50)
                    .IsRequired();

                emailBuilder.HasIndex(u => u.Value).HasDatabaseName("users_email_index").IsUnique();
            }
        );

        builder.OwnsOne(u => u.Password).Property(u => u.Value).HasMaxLength(72).IsRequired();

        builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(50).IsRequired();

        builder.Property(u => u.IsEmailVerified).HasDefaultValue(false).IsRequired();
        builder
            .Property(u => u.LastDateTimeOfSentVerificationCode)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.OwnsOne(
            u => u.EmailVerificationCode,
            emailVerificationCodeBuilder =>
            {
                emailVerificationCodeBuilder
                    .Property(u => u.Value)
                    .HasColumnName("EmailVerificationCode")
                    .HasMaxLength(EmailVerificationCode.MaxLength)
                    .IsRequired();
            }
        );

        builder
            .Property(u => u.RegistrationDate)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder
            .Property(u => u.SignOutDate)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();
    }
}
