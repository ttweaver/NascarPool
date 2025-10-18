using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePickId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RaceResults_Picks_PickId",
                table: "RaceResults");

            migrationBuilder.DropIndex(
                name: "IX_RaceResults_PickId",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "PickId",
                table: "RaceResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PickId",
                table: "RaceResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaceResults_PickId",
                table: "RaceResults",
                column: "PickId");

            migrationBuilder.AddForeignKey(
                name: "FK_RaceResults_Picks_PickId",
                table: "RaceResults",
                column: "PickId",
                principalTable: "Picks",
                principalColumn: "Id");
        }
    }
}
