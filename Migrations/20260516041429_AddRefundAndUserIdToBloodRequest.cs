using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BBMS.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundAndUserIdToBloodRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RefundRequestedAt",
                table: "BloodRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundStatus",
                table: "BloodRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundRequestedAt",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "RefundStatus",
                table: "BloodRequests");
        }
    }
}
