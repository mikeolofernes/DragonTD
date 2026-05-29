using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DragonTD.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GoldReward = table.Column<int>(type: "integer", nullable: false),
                    EssenceReward = table.Column<int>(type: "integer", nullable: false),
                    GemReward = table.Column<int>(type: "integer", nullable: false),
                    RewardTiersJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventDefinitions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EventDefinitions",
                columns: new[] { "Id", "Description", "DisplayName", "EndUtc", "EssenceReward", "EventId", "EventType", "GemReward", "GoldReward", "RewardTiersJson", "StartUtc" },
                values: new object[,]
                {
                    { 1, "Clear patrol objectives and claim a daily account boost.", "Daily Hunt", new DateTime(2030, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 35, "daily_hunt", "DailyClaimable", 0, 250, "[]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Short challenge preview with score tracking.", "Gem Rush", new DateTime(2030, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 0, "gem_rush", "ScoredChallenge", 0, 0, "[{\"ScoreThreshold\":100,\"GemReward\":10},{\"ScoreThreshold\":500,\"GemReward\":25},{\"ScoreThreshold\":1000,\"GemReward\":50}]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Locked until Clan/social backend is active.", "Clan Raid", new DateTime(2030, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 0, "clan_raid", "Locked", 0, 0, "[]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventDefinitions");
        }
    }
}
