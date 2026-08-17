using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amber.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailVerificationCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    LastDateTimeOfSentVerificationCode = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    Password_Value = table.Column<string>(type: "character varying(72)", maxLength: 72, nullable: true),
                    SignOutDate = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    GoogleId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sync_cells",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    tbl = table.Column<string>(type: "text", nullable: false),
                    row_id = table.Column<string>(type: "text", nullable: false),
                    col = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<byte[]>(type: "bytea", nullable: true),
                    Hlc = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    ServerSeq = table.Column<long>(type: "bigint", nullable: false),
                    WrittenAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_cells", x => new { x.UserId, x.tbl, x.row_id, x.col });
                    table.ForeignKey(
                        name: "FK_sync_cells_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sync_cells_UserId_ServerSeq",
                table: "sync_cells",
                columns: new[] { "UserId", "ServerSeq" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_cells_UserId_WrittenAt",
                table: "sync_cells",
                columns: new[] { "UserId", "WrittenAt" });

            migrationBuilder.CreateIndex(
                name: "users_email_index",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_google_id_index",
                table: "users",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "users_username_index",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_cells");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
