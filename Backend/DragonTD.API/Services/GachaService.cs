using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Services;

public class GachaService
{
    private readonly AppDbContext _db;
    private static readonly Random _rng = new();

    // Per-rarity drop rates for the standard banner
    private static readonly (DragonRarity Rarity, float Rate)[] BaseRates =
    {
        (DragonRarity.SSS, 0.005f),
        (DragonRarity.SS,  0.025f),
        (DragonRarity.S,   0.070f),
        (DragonRarity.A,   0.200f),
        (DragonRarity.B,   0.300f),
        (DragonRarity.C,   0.400f),
    };

    private const int SingleCost = 300;
    private const int TenCost    = 2700;
    private const int SoftPityAt = 50;
    private const int HardPityAt = 100;

    public GachaService(AppDbContext db) => _db = db;

    public async Task<GachaPullResponse> PullAsync(GachaPullRequest request)
    {
        var player = await _db.Players.FindAsync(request.PlayerId)
            ?? throw new KeyNotFoundException("Player not found");

        int pullCount = request.IsTenPull ? 10 : 1;
        int cost = request.IsTenPull ? TenCost : SingleCost;

        if (player.Gems < cost)
            throw new InvalidOperationException("Insufficient gems");

        var allDragons = await _db.DragonDefinitions
            .Where(d => d.IsAvailableInGacha)
            .ToListAsync();

        var results = new List<GachaPullResultItem>();
        bool guaranteedAPlus = request.IsTenPull;

        for (int i = 0; i < pullCount; i++)
        {
            player.GachaTotalPulls++;
            player.GachaPullsSinceLastS++;

            bool forceAPlus = guaranteedAPlus && i == pullCount - 1 && !results.Any(r => r.Rarity >= DragonRarity.A);
            DragonRarity rarity = RollRarity(player.GachaPullsSinceLastS, forceAPlus);

            if (rarity >= DragonRarity.S)
                player.GachaPullsSinceLastS = 0;

            DragonDefinition? picked = PickDragon(allDragons, rarity);
            if (picked == null) continue;

            bool isNew = !await _db.PlayerDragons.AnyAsync(pd => pd.PlayerId == request.PlayerId && pd.DragonDefinitionId == picked.Id);
            _db.PlayerDragons.Add(new PlayerDragon { PlayerId = request.PlayerId, DragonDefinitionId = picked.Id });
            results.Add(new GachaPullResultItem(picked.Id, picked.Name, picked.Rarity, isNew));
        }

        player.Gems -= cost;
        await _db.SaveChangesAsync();

        return new GachaPullResponse(
            results,
            GemsSpent: cost,
            PullsSinceLastS: player.GachaPullsSinceLastS,
            TotalPulls: player.GachaTotalPulls,
            GemsRemaining: player.Gems);
    }

    private static DragonRarity RollRarity(int pullsSinceLastS, bool forceAPlus)
    {
        if (pullsSinceLastS >= HardPityAt)
            return DragonRarity.SSS;

        // Soft pity: linearly boost S+ rates from pull 50 onward
        float softBoost = pullsSinceLastS >= SoftPityAt
            ? (pullsSinceLastS - SoftPityAt) / (float)(HardPityAt - SoftPityAt) * 0.5f
            : 0f;

        float roll = (float)_rng.NextDouble();
        float cumulative = 0f;

        foreach (var (rarity, rate) in BaseRates)
        {
            float adjusted = rate;
            if (rarity >= DragonRarity.S) adjusted = Mathf.Clamp01(rate + softBoost);
            cumulative += adjusted;
            if (roll < cumulative) return rarity;
        }

        return forceAPlus ? DragonRarity.A : DragonRarity.C;
    }

    private static DragonDefinition? PickDragon(List<DragonDefinition> pool, DragonRarity rarity)
    {
        var matching = pool.Where(d => d.Rarity == rarity).ToList();
        if (matching.Count == 0)
        {
            // Fallback: nearest rarity below
            matching = pool.Where(d => d.Rarity <= rarity).ToList();
        }
        if (matching.Count == 0) return null;
        return matching[_rng.Next(matching.Count)];
    }

    // Mathf replacement for non-Unity context
    private static class Mathf
    {
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
