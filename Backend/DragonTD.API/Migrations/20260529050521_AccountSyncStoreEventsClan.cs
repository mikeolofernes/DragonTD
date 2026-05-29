using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DragonTD.API.Migrations
{
    /// <inheritdoc />
    public partial class AccountSyncStoreEventsClan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DragonDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Lore = table.Column<string>(type: "text", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    Element = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    BaseHp = table.Column<float>(type: "real", nullable: false),
                    BaseAttack = table.Column<float>(type: "real", nullable: false),
                    BaseDefense = table.Column<float>(type: "real", nullable: false),
                    BaseSpeed = table.Column<float>(type: "real", nullable: false),
                    BaseRange = table.Column<float>(type: "real", nullable: false),
                    ManaCost = table.Column<int>(type: "integer", nullable: false),
                    IsAvailableInGacha = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DragonDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    FirebaseUid = table.Column<string>(type: "text", nullable: false),
                    Gems = table.Column<int>(type: "integer", nullable: false),
                    Gold = table.Column<int>(type: "integer", nullable: false),
                    PlayerLevel = table.Column<int>(type: "integer", nullable: false),
                    PlayerXp = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GachaPullsSinceLastS = table.Column<int>(type: "integer", nullable: false),
                    GachaTotalPulls = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventChallengeStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<string>(type: "text", nullable: false),
                    BestScore = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventChallengeStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventChallengeStates_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IapPurchaseReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: false),
                    Receipt = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    GemsGranted = table.Column<int>(type: "integer", nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IapPurchaseReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IapPurchaseReceipts_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDragons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    DragonDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    BondLevel = table.Column<int>(type: "integer", nullable: false),
                    BondXp = table.Column<float>(type: "real", nullable: false),
                    TotalBattles = table.Column<long>(type: "bigint", nullable: false),
                    ObtainedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDragons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerDragons_DragonDefinitions_DragonDefinitionId",
                        column: x => x.DragonDefinitionId,
                        principalTable: "DragonDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerDragons_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEventClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<string>(type: "text", nullable: false),
                    ClaimDateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEventClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerEventClaims_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerProgressionStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SaveJson = table.Column<string>(type: "text", nullable: false),
                    LastBattleRewardJson = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProgressionStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerProgressionStates_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DragonDefinitions",
                columns: new[] { "Id", "BaseAttack", "BaseDefense", "BaseHp", "BaseRange", "BaseSpeed", "Element", "IsAvailableInGacha", "Lore", "ManaCost", "Name", "Rarity", "Role" },
                values: new object[,]
                {
                    { 1, 180f, 80f, 1200f, 4f, 3.5f, 0, true, "A young fire dragon born from volcanic eruptions.", 80, "Ignarion", 3, 1 },
                    { 2, 100f, 120f, 1600f, 3f, 2.5f, 1, true, "A guardian of ocean depths who heals allies.", 70, "Aquariel", 2, 4 },
                    { 3, 220f, 60f, 900f, 5f, 5f, 4, true, "Lightning given form — strikes before the thunder.", 90, "Voltaris", 3, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventChallengeStates_PlayerId_EventId",
                table: "EventChallengeStates",
                columns: new[] { "PlayerId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IapPurchaseReceipts_PlayerId",
                table: "IapPurchaseReceipts",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_IapPurchaseReceipts_TransactionId",
                table: "IapPurchaseReceipts",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDragons_DragonDefinitionId",
                table: "PlayerDragons",
                column: "DragonDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDragons_PlayerId",
                table: "PlayerDragons",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEventClaims_PlayerId_EventId_ClaimDateUtc",
                table: "PlayerEventClaims",
                columns: new[] { "PlayerId", "EventId", "ClaimDateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProgressionStates_PlayerId",
                table: "PlayerProgressionStates",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_FirebaseUid",
                table: "Players",
                column: "FirebaseUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventChallengeStates");

            migrationBuilder.DropTable(
                name: "IapPurchaseReceipts");

            migrationBuilder.DropTable(
                name: "PlayerDragons");

            migrationBuilder.DropTable(
                name: "PlayerEventClaims");

            migrationBuilder.DropTable(
                name: "PlayerProgressionStates");

            migrationBuilder.DropTable(
                name: "DragonDefinitions");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
