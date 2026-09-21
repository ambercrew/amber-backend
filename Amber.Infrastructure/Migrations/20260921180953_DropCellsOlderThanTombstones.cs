using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amber.Infrastructure.Migrations
{
    /// <summary>
    /// Pushes now drop a row's cells older than its tombstone; this drops the ones stored
    /// before that. HLCs aren't zero-padded, so they're compared component-wise like Hlc.IsAfter.
    /// </summary>
    public partial class DropCellsOlderThanTombstones : Migration
    {
        private const string IsOlderThanTombstone = """
            EXISTS (
                SELECT 1 FROM sync_cells AS t
                WHERE t."UserId" = c."UserId"
                  AND t.tbl = c.tbl
                  AND t.row_id = c.row_id
                  AND t.col = '__deleted'
                  AND (
                      split_part(t."Hlc", '-', 1)::numeric,
                      ('x' || lpad(split_part(t."Hlc", '-', 2), 16, '0'))::bit(64)::bigint,
                      regexp_replace(t."Hlc", '^[^-]*-[^-]*-', '') COLLATE "C"
                  ) >= (
                      split_part(c."Hlc", '-', 1)::numeric,
                      ('x' || lpad(split_part(c."Hlc", '-', 2), 16, '0'))::bit(64)::bigint,
                      regexp_replace(c."Hlc", '^[^-]*-[^-]*-', '') COLLATE "C"
                  )
            )
            """;

        private const string IsUsersHighestServerSeq = """
            c."ServerSeq" = (SELECT MAX(m."ServerSeq") FROM sync_cells AS m WHERE m."UserId" = c."UserId")
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The next ServerSeq is the user's MAX, so their top cell is emptied, not deleted.
            migrationBuilder.Sql(
                $"""
                UPDATE sync_cells AS c
                SET "Value" = NULL,
                    "SizeInBytes" = c."SizeInBytes" - COALESCE(octet_length(c."Value"), 0)
                WHERE c.col <> '__deleted'
                  AND {IsUsersHighestServerSeq}
                  AND {IsOlderThanTombstone};
                """
            );

            migrationBuilder.Sql(
                $"""
                DELETE FROM sync_cells AS c
                WHERE c.col <> '__deleted'
                  AND NOT {IsUsersHighestServerSeq}
                  AND {IsOlderThanTombstone};
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
