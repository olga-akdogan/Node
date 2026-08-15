using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Node.Data.Migrations
{
    /// <inheritdoc />
    public partial class VoegGeslachtVoorkeurenEnLlmInterpretatiesToe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterpretationLanguage",
                table: "NatalCharts",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterpretationText",
                table: "NatalCharts",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerLookingForText",
                table: "NatalCharts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompatibilityExplanationLanguage",
                table: "Matches",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Male");

            migrationBuilder.CreateTable(
                name: "PartnerPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerPreferences_UserId_Gender",
                table: "PartnerPreferences",
                columns: new[] { "UserId", "Gender" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerPreferences");

            migrationBuilder.DropColumn(
                name: "InterpretationLanguage",
                table: "NatalCharts");

            migrationBuilder.DropColumn(
                name: "InterpretationText",
                table: "NatalCharts");

            migrationBuilder.DropColumn(
                name: "PartnerLookingForText",
                table: "NatalCharts");

            migrationBuilder.DropColumn(
                name: "CompatibilityExplanationLanguage",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AspNetUsers");
        }
    }
}
