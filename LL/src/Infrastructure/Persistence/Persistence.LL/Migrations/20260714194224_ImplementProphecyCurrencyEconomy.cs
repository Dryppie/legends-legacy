using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ImplementProphecyCurrencyEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProphecyCurrencyConversionVersion",
                table: "Entities",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyProphecyRerollStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RerollsUsed = table.Column<int>(type: "integer", nullable: false),
                    FateEchoSpent = table.Column<long>(type: "bigint", nullable: false),
                    ShownDefinitionIdsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyProphecyRerollStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyProphecyRerollStates_PlayerId_CharacterId_PeriodStart",
                table: "DailyProphecyRerollStates",
                columns: new[] { "PlayerId", "CharacterId", "PeriodStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyProphecyRerollStates");

            migrationBuilder.DropColumn(
                name: "ProphecyCurrencyConversionVersion",
                table: "Entities");
        }
    }
}
