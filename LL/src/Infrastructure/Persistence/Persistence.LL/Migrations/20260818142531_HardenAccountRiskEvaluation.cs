using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class HardenAccountRiskEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnalysisWindowStart",
                table: "AccountRiskSnapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "AnalyzedTransferCount",
                table: "AccountRiskSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EvidenceComplete",
                table: "AccountRiskSnapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnalysisWindowStart",
                table: "AccountRiskHistory",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "AnalyzedTransferCount",
                table: "AccountRiskHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EvidenceComplete",
                table: "AccountRiskHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisWindowStart",
                table: "AccountRiskSnapshots");

            migrationBuilder.DropColumn(
                name: "AnalyzedTransferCount",
                table: "AccountRiskSnapshots");

            migrationBuilder.DropColumn(
                name: "EvidenceComplete",
                table: "AccountRiskSnapshots");

            migrationBuilder.DropColumn(
                name: "AnalysisWindowStart",
                table: "AccountRiskHistory");

            migrationBuilder.DropColumn(
                name: "AnalyzedTransferCount",
                table: "AccountRiskHistory");

            migrationBuilder.DropColumn(
                name: "EvidenceComplete",
                table: "AccountRiskHistory");
        }
    }
}
