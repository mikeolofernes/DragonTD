using DragonTD.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<DragonDefinition> DragonDefinitions => Set<DragonDefinition>();
    public DbSet<PlayerDragon> PlayerDragons => Set<PlayerDragon>();
    public DbSet<PlayerProgressionState> PlayerProgressionStates => Set<PlayerProgressionState>();
    public DbSet<IapPurchaseReceipt> IapPurchaseReceipts => Set<IapPurchaseReceipt>();
    public DbSet<EventChallengeState> EventChallengeStates => Set<EventChallengeState>();
    public DbSet<PlayerEventClaim> PlayerEventClaims => Set<PlayerEventClaim>();
    public DbSet<EventDefinition> EventDefinitions => Set<EventDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(e =>
        {
            e.HasIndex(p => p.FirebaseUid).IsUnique();
            e.HasIndex(p => p.Username).IsUnique();
        });

        modelBuilder.Entity<PlayerProgressionState>(e =>
        {
            e.HasIndex(p => p.PlayerId).IsUnique();
            e.HasOne(p => p.Player)
             .WithOne()
             .HasForeignKey<PlayerProgressionState>(p => p.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IapPurchaseReceipt>(e =>
        {
            e.HasIndex(p => p.TransactionId).IsUnique();
            e.HasOne(p => p.Player)
             .WithMany()
             .HasForeignKey(p => p.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventChallengeState>(e =>
        {
            e.HasIndex(p => new { p.PlayerId, p.EventId }).IsUnique();
            e.HasOne(p => p.Player)
             .WithMany()
             .HasForeignKey(p => p.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerEventClaim>(e =>
        {
            e.HasIndex(p => new { p.PlayerId, p.EventId, p.ClaimDateUtc }).IsUnique();
            e.HasOne(p => p.Player)
             .WithMany()
             .HasForeignKey(p => p.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerDragon>(e =>
        {
            e.HasOne(pd => pd.Player)
             .WithMany(p => p.Dragons)
             .HasForeignKey(pd => pd.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pd => pd.DragonDefinition)
             .WithMany()
             .HasForeignKey(pd => pd.DragonDefinitionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DragonDefinition>().HasData(
            new DragonDefinition { Id = 1, Name = "Ignarion", Lore = "A young fire dragon born from volcanic eruptions.", Rarity = DragonRarity.S, Element = DragonElement.Fire, Role = DragonRole.Striker, BaseHp = 1200f, BaseAttack = 180f, BaseDefense = 80f, BaseSpeed = 3.5f, BaseRange = 4f, ManaCost = 80 },
            new DragonDefinition { Id = 2, Name = "Aquariel", Lore = "A guardian of ocean depths who heals allies.", Rarity = DragonRarity.A, Element = DragonElement.Water, Role = DragonRole.Guardian, BaseHp = 1600f, BaseAttack = 100f, BaseDefense = 120f, BaseSpeed = 2.5f, BaseRange = 3f, ManaCost = 70 },
            new DragonDefinition { Id = 3, Name = "Voltaris", Lore = "Lightning given form — strikes before the thunder.", Rarity = DragonRarity.S, Element = DragonElement.Lightning, Role = DragonRole.Tempest, BaseHp = 900f, BaseAttack = 220f, BaseDefense = 60f, BaseSpeed = 5f, BaseRange = 5f, ManaCost = 90 }
        );

        modelBuilder.Entity<EventDefinition>().HasData(
            new EventDefinition
            {
                Id = 1,
                EventId = "daily_hunt",
                DisplayName = "Daily Hunt",
                Description = "Clear patrol objectives and claim a daily account boost.",
                EventType = "DailyClaimable",
                StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                GoldReward = 250,
                EssenceReward = 35,
                GemReward = 0,
                RewardTiersJson = "[]"
            },
            new EventDefinition
            {
                Id = 2,
                EventId = "gem_rush",
                DisplayName = "Gem Rush",
                Description = "Short challenge preview with score tracking.",
                EventType = "ScoredChallenge",
                StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                GoldReward = 0,
                EssenceReward = 0,
                GemReward = 0,
                RewardTiersJson = "[{\"ScoreThreshold\":100,\"GemReward\":10},{\"ScoreThreshold\":500,\"GemReward\":25},{\"ScoreThreshold\":1000,\"GemReward\":50}]"
            },
            new EventDefinition
            {
                Id = 3,
                EventId = "clan_raid",
                DisplayName = "Clan Raid",
                Description = "Locked until Clan/social backend is active.",
                EventType = "Locked",
                StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                GoldReward = 0,
                EssenceReward = 0,
                GemReward = 0,
                RewardTiersJson = "[]"
            }
        );
    }
}
