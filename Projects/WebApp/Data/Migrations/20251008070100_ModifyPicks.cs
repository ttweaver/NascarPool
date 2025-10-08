using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyPicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Picks_Drivers_DriverId",
                table: "Picks");

            migrationBuilder.DropForeignKey(
                name: "FK_Picks_Pools_PoolId",
                table: "Picks");

            migrationBuilder.DropForeignKey(
                name: "FK_Races_Pools_PoolId",
                table: "Races");

            migrationBuilder.RenameColumn(
                name: "PoolId",
                table: "Picks",
                newName: "Pick3Id");

            migrationBuilder.RenameColumn(
                name: "DriverId",
                table: "Picks",
                newName: "Pick2Id");

            migrationBuilder.RenameIndex(
                name: "IX_Picks_PoolId",
                table: "Picks",
                newName: "IX_Picks_Pick3Id");

            migrationBuilder.RenameIndex(
                name: "IX_Picks_DriverId",
                table: "Picks",
                newName: "IX_Picks_Pick2Id");

            migrationBuilder.AlterColumn<int>(
                name: "PoolId",
                table: "Races",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pick1Id",
                table: "Picks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Picks_Pick1Id",
                table: "Picks",
                column: "Pick1Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Picks_Drivers_Pick1Id",
                table: "Picks",
                column: "Pick1Id",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Picks_Drivers_Pick2Id",
                table: "Picks",
                column: "Pick2Id",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Picks_Drivers_Pick3Id",
                table: "Picks",
                column: "Pick3Id",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Races_Pools_PoolId",
                table: "Races",
                column: "PoolId",
                principalTable: "Pools",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Picks_Drivers_Pick1Id",
                table: "Picks");

            migrationBuilder.DropForeignKey(
                name: "FK_Picks_Drivers_Pick2Id",
                table: "Picks");

            migrationBuilder.DropForeignKey(
                name: "FK_Picks_Drivers_Pick3Id",
                table: "Picks");

            migrationBuilder.DropForeignKey(
                name: "FK_Races_Pools_PoolId",
                table: "Races");

            migrationBuilder.DropIndex(
                name: "IX_Picks_Pick1Id",
                table: "Picks");

            migrationBuilder.DropColumn(
                name: "Pick1Id",
                table: "Picks");

            migrationBuilder.RenameColumn(
                name: "Pick3Id",
                table: "Picks",
                newName: "PoolId");

            migrationBuilder.RenameColumn(
                name: "Pick2Id",
                table: "Picks",
                newName: "DriverId");

            migrationBuilder.RenameIndex(
                name: "IX_Picks_Pick3Id",
                table: "Picks",
                newName: "IX_Picks_PoolId");

            migrationBuilder.RenameIndex(
                name: "IX_Picks_Pick2Id",
                table: "Picks",
                newName: "IX_Picks_DriverId");

            migrationBuilder.AlterColumn<int>(
                name: "PoolId",
                table: "Races",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Picks_Drivers_DriverId",
                table: "Picks",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Picks_Pools_PoolId",
                table: "Picks",
                column: "PoolId",
                principalTable: "Pools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Races_Pools_PoolId",
                table: "Races",
                column: "PoolId",
                principalTable: "Pools",
                principalColumn: "Id");
        }
    }
}
