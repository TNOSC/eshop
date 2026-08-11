using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeadLetters_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dead_letters",
                schema: "outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handler = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dead_lettered_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    replay_count = table.Column<int>(type: "integer", nullable: false),
                    last_replayed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replayed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dead_letters_pending",
                schema: "outbox",
                table: "dead_letters",
                column: "dead_lettered_on_utc",
                filter: "replayed_on_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letters",
                schema: "outbox");
        }
    }
}
