using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BBMS.Migrations
{
    /// <inheritdoc />
    public partial class AddAgeAndTotalDonations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "DonateBloods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDonations",
                table: "DonateBloods",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "DonateBloods");

            migrationBuilder.DropColumn(
                name: "TotalDonations",
                table: "DonateBloods");
        }
    }
}
