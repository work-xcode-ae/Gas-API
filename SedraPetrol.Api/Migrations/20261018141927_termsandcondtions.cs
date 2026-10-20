using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class termsandcondtions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contract_term_template",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    text_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    text_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_term_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supply_contract_term",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    template_id = table.Column<long>(type: "bigint", nullable: true),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_custom = table.Column<bool>(type: "bit", nullable: false),
                    is_modified = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_contract_term", x => x.id);
                    table.ForeignKey(
                        name: "FK_supply_contract_term_contract_term_template_template_id",
                        column: x => x.template_id,
                        principalTable: "contract_term_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supply_contract_term_supply_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "supply_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supply_contract_term_contract_id",
                table: "supply_contract_term",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_supply_contract_term_template_id",
                table: "supply_contract_term",
                column: "template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supply_contract_term");

            migrationBuilder.DropTable(
                name: "contract_term_template");
        }
    }
}
