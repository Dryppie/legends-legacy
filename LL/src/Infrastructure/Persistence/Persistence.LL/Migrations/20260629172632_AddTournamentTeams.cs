using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTeamOwner",
                table: "TournamentParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "TournamentParticipants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TournamentTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OwnerParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MemberCount = table.Column<int>(type: "integer", nullable: false),
                    EliminatedInRoundNumber = table.Column<int>(type: "integer", nullable: true),
                    FinalPlacement = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTeamApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeamApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeamApplications_TournamentTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTeamInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviterParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeamInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeamInvites_TournamentTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TeamId",
                table: "TournamentParticipants",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_TeamId",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamApplications_TeamId_ApplicantParticipantId_St~",
                table: "TournamentTeamApplications",
                columns: new[] { "TeamId", "ApplicantParticipantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamApplications_TournamentId_ApplicantParticipan~",
                table: "TournamentTeamApplications",
                columns: new[] { "TournamentId", "ApplicantParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamInvites_TeamId_InvitedParticipantId_Status",
                table: "TournamentTeamInvites",
                columns: new[] { "TeamId", "InvitedParticipantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamInvites_TournamentId_InvitedParticipantId",
                table: "TournamentTeamInvites",
                columns: new[] { "TournamentId", "InvitedParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_OwnerParticipantId",
                table: "TournamentTeams",
                column: "OwnerParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Name",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Seed",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Seed" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Status",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentParticipants_TournamentTeams_TeamId",
                table: "TournamentParticipants",
                column: "TeamId",
                principalTable: "TournamentTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentParticipants_TournamentTeams_TeamId",
                table: "TournamentParticipants");

            migrationBuilder.DropTable(
                name: "TournamentTeamApplications");

            migrationBuilder.DropTable(
                name: "TournamentTeamInvites");

            migrationBuilder.DropTable(
                name: "TournamentTeams");

            migrationBuilder.DropIndex(
                name: "IX_TournamentParticipants_TeamId",
                table: "TournamentParticipants");

            migrationBuilder.DropIndex(
                name: "IX_TournamentParticipants_TournamentId_TeamId",
                table: "TournamentParticipants");

            migrationBuilder.DropColumn(
                name: "IsTeamOwner",
                table: "TournamentParticipants");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "TournamentParticipants");
        }
    }
}
