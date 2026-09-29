using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleSupportCaseFollowUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FollowUpAt",
                table: "SupportCases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextAction",
                table: "SupportCases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "SupportCases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SupportCases_Status_FollowUpAt",
                table: "SupportCases",
                columns: new[] { "Status", "FollowUpAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupportCases_Status_FollowUpAt",
                table: "SupportCases");

            migrationBuilder.DropColumn(
                name: "FollowUpAt",
                table: "SupportCases");

            migrationBuilder.DropColumn(
                name: "NextAction",
                table: "SupportCases");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "SupportCases");
        }
    }
}
