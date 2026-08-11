using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Idempotency_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "idempotency");

            migrationBuilder.CreateTable(
                name: "processed_events",
                schema: "outbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handler = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_events", x => new { x.event_id, x.handler });
                });

            migrationBuilder.CreateTable(
                name: "requests",
                schema: "idempotency",
                columns: table => new
                {
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    handler = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    request_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    response = table.Column<string>(type: "jsonb", nullable: true),
                    response_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requests", x => new { x.idempotency_key, x.handler });
                });

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_processed_on",
                schema: "outbox",
                table: "processed_events",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "ix_requests_expires_on",
                schema: "idempotency",
                table: "requests",
                column: "expires_on_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "outbox");

            migrationBuilder.DropTable(
                name: "requests",
                schema: "idempotency");
        }
    }
}
