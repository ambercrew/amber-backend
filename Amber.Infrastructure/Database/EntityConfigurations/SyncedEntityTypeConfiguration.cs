using Amber.Domain.Sync.Entities;
using Amber.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amber.Infrastructure.Database.EntityConfigurations;

public class SyncEntityTypeConfiguration : IEntityTypeConfiguration<SyncedEntity>
{
    public void Configure(EntityTypeBuilder<SyncedEntity> builder)
    {
        builder.ToTable("synced_entities");

        builder.HasKey(s => new { s.UserId, s.EntityId });
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.EntityId).IsRequired();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.LastSyncDate).HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(s => new { s.UserId, s.LastSyncDate });

        builder.Property(s => s.CreatedDate).HasColumnType("timestamptz").IsRequired();

        builder.Property(s => s.EntityType).IsRequired();

        builder.Property(s => s.Data).HasColumnType("bytea").IsRequired(false);

        builder.Property(s => s.SizeInBytes).IsRequired();
    }
}
