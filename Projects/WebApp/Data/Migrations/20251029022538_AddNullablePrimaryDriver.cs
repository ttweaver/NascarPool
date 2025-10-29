using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNullablePrimaryDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverFirstHalfId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PrimaryDriverFirstHalfId",
                table: "AspNetUsers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverFirstHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverFirstHalfId",
                principalTable: "Drivers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverSecondHalfId",
                principalTable: "Drivers",
                principalColumn: "Id");
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

            migrationBuilder.AlterColumn<int>(
                name: "PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PrimaryDriverFirstHalfId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverFirstHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverFirstHalfId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverSecondHalfId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
