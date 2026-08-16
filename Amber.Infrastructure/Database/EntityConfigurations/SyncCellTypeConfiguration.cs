using Amber.Domain.Sync.Entities;
using Amber.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amber.Infrastructure.Database.EntityConfigurations;

public class SyncCellTypeConfiguration : IEntityTypeConfiguration<SyncCell>
{
    public void Configure(EntityTypeBuilder<SyncCell> builder)
    {
        builder.ToTable("sync_cells");

        // SyncCell.Id bundles these into a SyncCellId value object for domain code, but EF Core
        // doesn't support complex/owned properties participating in a key, so the mapped key
        // columns stay flat scalar properties; builder.Ignore(c => c.Id) below keeps EF from
        // trying (and failing) to map that derived property on its own.
        builder.HasKey(c => new
        {
            c.UserId,
            c.Table,
            c.RowId,
            c.Column,
        });
        builder.Ignore(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Table).HasColumnName("tbl").IsRequired();
        builder.Property(c => c.RowId).HasColumnName("row_id").IsRequired();
        builder.Property(c => c.Column).HasColumnName("col").IsRequired();
        builder.Property(c => c.Value).HasColumnType("bytea").IsRequired(false);
        builder.Property(c => c.DeviceId).IsRequired();
        builder.Property(c => c.ServerSeq).IsRequired();
        builder.Property(c => c.WrittenAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.SizeInBytes).IsRequired();

        builder.OwnsOne(
            c => c.Hlc,
            hlcBuilder =>
            {
                hlcBuilder.Property(h => h.Value).HasColumnName("Hlc").IsRequired();
            }
        );

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.UserId, c.ServerSeq });
        builder.HasIndex(c => new { c.UserId, c.WrittenAt });
    }
}
