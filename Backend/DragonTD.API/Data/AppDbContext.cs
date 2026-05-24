using DragonTD.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<DragonDefinition> DragonDefinitions => Set<DragonDefinition>();
    public DbSet<PlayerDragon> PlayerDragons => Set<PlayerDragon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(e =>
        {
            e.HasIndex(p => p.FirebaseUid).IsUnique();
            e.HasIndex(p => p.Username).IsUnique();
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
    }
}
