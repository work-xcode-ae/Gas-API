using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class PrintLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cash_contact_mobile",
                table: "company",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cash_contact_name",
                table: "company",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_care_phone",
                table: "company",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fax",
                table: "company",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_payment_note",
                table: "company",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "letterhead_path",
                table: "company",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "po_box",
                table: "company",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signatory_name_ar",
                table: "company",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signatory_name_en",
                table: "company",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signatory_title_ar",
                table: "company",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signatory_title_en",
                table: "company",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_invoice",
                table: "bank_account",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "swift_code",
                table: "bank_account",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cash_contact_mobile",
                table: "company");

            migrationBuilder.DropColumn(
                name: "cash_contact_name",
                table: "company");

            migrationBuilder.DropColumn(
                name: "customer_care_phone",
                table: "company");

            migrationBuilder.DropColumn(
                name: "fax",
                table: "company");

            migrationBuilder.DropColumn(
                name: "invoice_payment_note",
                table: "company");

            migrationBuilder.DropColumn(
                name: "letterhead_path",
                table: "company");

            migrationBuilder.DropColumn(
                name: "po_box",
                table: "company");

            migrationBuilder.DropColumn(
                name: "signatory_name_ar",
                table: "company");

            migrationBuilder.DropColumn(
                name: "signatory_name_en",
                table: "company");

            migrationBuilder.DropColumn(
                name: "signatory_title_ar",
                table: "company");

            migrationBuilder.DropColumn(
                name: "signatory_title_en",
                table: "company");

            migrationBuilder.DropColumn(
                name: "show_on_invoice",
                table: "bank_account");

            migrationBuilder.DropColumn(
                name: "swift_code",
                table: "bank_account");
        }
    }
}
