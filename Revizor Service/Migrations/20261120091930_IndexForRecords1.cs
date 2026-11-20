using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revizor_Service.Migrations
{
    /// <inheritdoc />
    public partial class IndexForRecords1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_PlanAssigmentId",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "idx_unique_planAssigment",
                table: "Records",
                column: "PlanAssigmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_unique_planAssigment",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "IX_Records_PlanAssigmentId",
                table: "Records",
                column: "PlanAssigmentId");
        }
    }
}
