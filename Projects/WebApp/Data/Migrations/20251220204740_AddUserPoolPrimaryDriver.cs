using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPoolPrimaryDriver : Migration
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

            migrationBuilder.CreateTable(
                name: "UserPoolPrimaryDrivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PoolId = table.Column<int>(type: "int", nullable: false),
                    PrimaryDriverFirstHalfId = table.Column<int>(type: "int", nullable: true),
                    PrimaryDriverSecondHalfId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPoolPrimaryDrivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPoolPrimaryDrivers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverFirstHalfId",
                        column: x => x.PrimaryDriverFirstHalfId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPoolPrimaryDrivers_Drivers_PrimaryDriverSecondHalfId",
                        column: x => x.PrimaryDriverSecondHalfId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPoolPrimaryDrivers_Pools_PoolId",
                        column: x => x.PoolId,
                        principalTable: "Pools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPoolPrimaryDrivers_PoolId",
                table: "UserPoolPrimaryDrivers",
                column: "PoolId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPoolPrimaryDrivers_PrimaryDriverFirstHalfId",
                table: "UserPoolPrimaryDrivers",
                column: "PrimaryDriverFirstHalfId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPoolPrimaryDrivers_PrimaryDriverSecondHalfId",
                table: "UserPoolPrimaryDrivers",
                column: "PrimaryDriverSecondHalfId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPoolPrimaryDrivers_UserId",
                table: "UserPoolPrimaryDrivers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPoolPrimaryDrivers");

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
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Drivers_PrimaryDriverSecondHalfId",
                table: "AspNetUsers",
                column: "PrimaryDriverSecondHalfId",
                principalTable: "Drivers",
                principalColumn: "Id");
        }
    }
}
