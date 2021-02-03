using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AutomaticStockTrader.Repository.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StratagysStocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Strategy = table.Column<string>(type: "varchar(50)", nullable: true),
                    StockSymbol = table.Column<string>(type: "varchar(10)", nullable: true),
                    TradingFrequency = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StratagysStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderPlaced = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AttemptedSharesBought = table.Column<long>(type: "bigint", nullable: false),
                    AttemptedCostPerShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualSharesBought = table.Column<long>(type: "bigint", nullable: true),
                    ActualCostPerShare = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_StratagysStocks_PositionId",
                        column: x => x.PositionId,
                        principalTable: "StratagysStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PositionId",
                table: "Orders",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_StratagysStocks_StockSymbol_Strategy_TradingFrequency",
                table: "StratagysStocks",
                columns: new[] { "StockSymbol", "Strategy", "TradingFrequency" },
                unique: true,
                filter: "[StockSymbol] IS NOT NULL AND [Strategy] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "StratagysStocks");
        }
    }
}
