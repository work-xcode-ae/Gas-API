using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_trail",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    table_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    entity_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    changes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    http_method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    request_path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_trail", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_trail_entity",
                table: "audit_trail",
                columns: new[] { "table_name", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_trail_occurred",
                table: "audit_trail",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trail_tx",
                table: "audit_trail",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trail_user",
                table: "audit_trail",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_trail");
        }
    }
}
