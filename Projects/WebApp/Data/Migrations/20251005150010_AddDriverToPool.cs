using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverToPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PoolId",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_PoolId",
                table: "Drivers",
                column: "PoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Pools_PoolId",
                table: "Drivers",
                column: "PoolId",
                principalTable: "Pools",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Pools_PoolId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_PoolId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "PoolId",
                table: "Drivers");
        }
    }
}
