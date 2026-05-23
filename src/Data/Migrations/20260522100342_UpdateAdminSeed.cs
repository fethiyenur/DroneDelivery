using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DroneKurye.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$92qlBGHrxEBDPSGDMonlgOqnHyAKAEjRFhgcCkHuBdVBbk16aXNUu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$K.WznmyHWdXVbBIi.plXq.6Nd4q8HFWrqVQUhgWYL7bDEmfELNnDS");
        }
    }
}
