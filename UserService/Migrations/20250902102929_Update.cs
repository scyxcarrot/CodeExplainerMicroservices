using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "A1A1A1A1-1A1A-1A1A-1A1A-1A1A1d1A1A1A", null, "Admin", "ADMIN" },
                    { "B2B2B2B2-2B2B-2B2B-2B2B-2B2B2d2B2B2B", null, "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "A1A1A1A1-1A1A-1A1A-1A1A-1A1A1d1A1A1A");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "B2B2B2B2-2B2B-2B2B-2B2B-2B2B2d2B2B2B");
        }
    }
}
