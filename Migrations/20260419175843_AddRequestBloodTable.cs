using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BBMS.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestBloodTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MobilePaymentNumber",
                table: "RequestBloods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "RequestBloods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "RequestBloods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalExpenseAmount",
                table: "RequestBloods",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MobilePaymentNumber",
                table: "RequestBloods");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "RequestBloods");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "RequestBloods");

            migrationBuilder.DropColumn(
                name: "TotalExpenseAmount",
                table: "RequestBloods");
        }
    }
}
