using System.Threading.Tasks;

namespace DragonTD.Core
{
    public interface IProgressionPersistenceService
    {
        string ModeLabel { get; }
        Task<ProgressionPersistenceResult> LoadProgressionAsync();
        Task<ProgressionPersistenceResult> SaveProgressionAsync(PlayerProgressionSaveData saveData);
        Task<ProgressionPersistenceResult> SyncBattleRewardAsync(PlayerProgressionApiDto progression, BattleRewardResult reward);
        void ResetProgression();
    }
}
