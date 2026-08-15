using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Node.Data.Migrations
{
    /// <inheritdoc />
    public partial class EchteHoroscoopBerekening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AscendantIsApproximate",
                table: "NatalCharts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BirthTimeIsUnknown",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AscendantIsApproximate",
                table: "NatalCharts");

            migrationBuilder.DropColumn(
                name: "BirthTimeIsUnknown",
                table: "AspNetUsers");
        }
    }
}
