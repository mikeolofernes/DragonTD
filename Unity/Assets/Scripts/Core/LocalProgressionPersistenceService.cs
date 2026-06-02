using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace DragonTD.Core
{
    public class LocalProgressionPersistenceService : IProgressionPersistenceService
    {
        private readonly string _savePath;

        public string ModeLabel => "Local";

        public LocalProgressionPersistenceService(string savePath)
        {
            _savePath = savePath;
        }

        public Task<ProgressionPersistenceResult> LoadProgressionAsync()
        {
            try
            {
                if (!File.Exists(_savePath))
                    return Task.FromResult(ProgressionPersistenceResult.Failure("No local save"));

                string json = File.ReadAllText(_savePath);
                var saveData = JsonUtility.FromJson<PlayerProgressionSaveData>(json);
                return Task.FromResult(saveData == null
                    ? ProgressionPersistenceResult.Failure("Local save was empty")
                    : ProgressionPersistenceResult.Success("Loaded local save", saveData));
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(ProgressionPersistenceResult.Failure($"Load failed: {ex.Message}"));
            }
        }

        public Task<ProgressionPersistenceResult> SaveProgressionAsync(PlayerProgressionSaveData saveData)
        {
            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(_savePath, json);
                return Task.FromResult(ProgressionPersistenceResult.Success("Saved locally", saveData));
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(ProgressionPersistenceResult.Failure($"Save failed: {ex.Message}"));
            }
        }

        public Task<ProgressionPersistenceResult> SyncBattleRewardAsync(PlayerProgressionApiDto progression, BattleRewardResult reward)
        {
            return Task.FromResult(ProgressionPersistenceResult.Success("Battle reward synced locally"));
        }

        public void ResetProgression()
        {
            if (File.Exists(_savePath))
                File.Delete(_savePath);
        }
    }
}
