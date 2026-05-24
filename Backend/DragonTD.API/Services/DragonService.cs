using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Services;

public class DragonService
{
    private readonly AppDbContext _db;

    public DragonService(AppDbContext db) => _db = db;

    public async Task<List<DragonDefinition>> GetAllDefinitionsAsync() =>
        await _db.DragonDefinitions.ToListAsync();

    public async Task<List<PlayerDragon>> GetPlayerDragonsAsync(int playerId) =>
        await _db.PlayerDragons
            .Include(pd => pd.DragonDefinition)
            .Where(pd => pd.PlayerId == playerId)
            .ToListAsync();

    public async Task<PlayerDragon> AddDragonToPlayerAsync(int playerId, int dragonDefinitionId)
    {
        var entry = new PlayerDragon { PlayerId = playerId, DragonDefinitionId = dragonDefinitionId };
        _db.PlayerDragons.Add(entry);
        await _db.SaveChangesAsync();
        return await _db.PlayerDragons.Include(pd => pd.DragonDefinition).FirstAsync(pd => pd.Id == entry.Id);
    }

    public async Task UpdateBondAsync(int playerDragonId, float bondXpGain)
    {
        var pd = await _db.PlayerDragons.FindAsync(playerDragonId)
            ?? throw new KeyNotFoundException("PlayerDragon not found");
        pd.TotalBattles++;
        pd.BondXp += bondXpGain;
        float threshold = 100f * pd.BondLevel;
        while (pd.BondXp >= threshold && pd.BondLevel < 20)
        {
            pd.BondXp -= threshold;
            pd.BondLevel++;
            threshold = 100f * pd.BondLevel;
        }
        await _db.SaveChangesAsync();
    }
}
