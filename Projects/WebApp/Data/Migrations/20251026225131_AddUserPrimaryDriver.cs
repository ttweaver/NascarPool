using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPrimaryDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryDriverFirstHalfId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PrimaryDriverFirstHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverFirstHalfId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverSecondHalfId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverFirstHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverFirstHalfId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverSecondHalfId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverFirstHalfId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PrimaryDriverFirstHalfId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrimaryDriverFirstHalfId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrimaryDriverSecondHalfId",
                table: "AspNetUsers");
        }
    }
}
