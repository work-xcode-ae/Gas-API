using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class VisitTechnicianFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customer_feedback",
                table: "maintenance_visit",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "customer_rating",
                table: "maintenance_visit",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "feedback_by",
                table: "maintenance_visit",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "technician_employee_id",
                table: "maintenance_visit",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_visit_technician_employee_id",
                table: "maintenance_visit",
                column: "technician_employee_id");

            migrationBuilder.AddForeignKey(
                name: "FK_maintenance_visit_employee_technician_employee_id",
                table: "maintenance_visit",
                column: "technician_employee_id",
                principalTable: "employee",
                principalColumn: "party_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_maintenance_visit_employee_technician_employee_id",
                table: "maintenance_visit");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_visit_technician_employee_id",
                table: "maintenance_visit");

            migrationBuilder.DropColumn(
                name: "customer_feedback",
                table: "maintenance_visit");

            migrationBuilder.DropColumn(
                name: "customer_rating",
                table: "maintenance_visit");

            migrationBuilder.DropColumn(
                name: "feedback_by",
                table: "maintenance_visit");

            migrationBuilder.DropColumn(
                name: "technician_employee_id",
                table: "maintenance_visit");
        }
    }
}
