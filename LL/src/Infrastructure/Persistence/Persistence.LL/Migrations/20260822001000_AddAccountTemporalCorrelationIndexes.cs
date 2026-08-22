using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.LL;

#nullable disable

namespace Persistence.LL.Migrations;

[DbContext(typeof(LLDbContext))]
[Migration("20260822001000_AddAccountTemporalCorrelationIndexes")]
public partial class AddAccountTemporalCorrelationIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId_CreatedUtc",
            table: "RefreshTokens",
            columns: new[] { "UserId", "CreatedUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_UserId_CreatedUtc",
            table: "RefreshTokens");
    }
}
