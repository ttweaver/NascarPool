using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPoolPrimaryDriverConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverFirstHalfId",
                table: "UserPoolPrimaryDrivers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverSecondHalfId",
                table: "UserPoolPrimaryDrivers");

            migrationBuilder.DropIndex(
                name: "IX_UserPoolPrimaryDrivers_UserId",
                table: "UserPoolPrimaryDrivers");

            migrationBuilder.CreateIndex(
                name: "IX_UserPoolPrimaryDrivers_UserId_PoolId",
                table: "UserPoolPrimaryDrivers",
                columns: new[] { "UserId", "PoolId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverFirstHalfId",
                table: "UserPoolPrimaryDrivers",
                column: "PrimaryDriverFirstHalfId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverSecondHalfId",
                table: "UserPoolPrimaryDrivers",
                column: "PrimaryDriverSecondHalfId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverFirstHalfId",
                table: "UserPoolPrimaryDrivers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverSecondHalfId",
                table: "UserPoolPrimaryDrivers");

            migrationBuilder.DropIndex(
                name: "IX_UserPoolPrimaryDrivers_UserId_PoolId",
                table: "UserPoolPrimaryDrivers");

            migrationBuilder.CreateIndex(
                name: "IX_UserPoolPrimaryDrivers_UserId",
                table: "UserPoolPrimaryDrivers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverFirstHalfId",
                table: "UserPoolPrimaryDrivers",
                column: "PrimaryDriverFirstHalfId",
                principalTable: "Drivers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverSecondHalfId",
                table: "UserPoolPrimaryDrivers",
                column: "PrimaryDriverSecondHalfId",
                principalTable: "Drivers",
                principalColumn: "Id");
        }
    }
}
