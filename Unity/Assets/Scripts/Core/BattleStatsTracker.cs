using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DragonTD.Core
{
    public class BattleStatsTracker : MonoBehaviour
    {
        public static BattleStatsTracker Instance { get; private set; }

        private readonly Dictionary<string, float> _waveDamageByDragon = new();
        private int _activeWave;
        private int _waveTowersPlaced;
        private int _waveManaSpent;
        private int _waveManaRefunded;
        private int _waveSkillsCast;
        private int _waveEnemiesKilled;
        private int _waveEnemiesLeaked;
        private string _latestSummary = string.Empty;
        private readonly Dictionary<string, float> _battleDamageByDragon = new();
        private int _battleTowersPlaced;
        private int _battleManaSpent;
        private int _battleManaRefunded;
        private int _battleSkillsCast;
        private int _battleEnemiesKilled;
        private int _battleEnemiesLeaked;
        private int _battleGoldRewards;
        private int _battleManaRewards;
        private int _battleFusions;
        private int _battleHybridFusions;
        private readonly Dictionary<string, float> _battleFusedDamageByDragon = new();

        public string LatestSummary => _latestSummary;
        public event System.Action<string> OnWaveSummary;

        public static BattleStatsTracker Ensure()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("BattleStatsTracker");
            return go.AddComponent<BattleStatsTracker>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetBattle()
        {
            _activeWave = 0;
            _latestSummary = string.Empty;
            ResetWaveCounters();
            _battleDamageByDragon.Clear();
            _battleTowersPlaced = 0;
            _battleManaSpent = 0;
            _battleManaRefunded = 0;
            _battleSkillsCast = 0;
            _battleEnemiesKilled = 0;
            _battleEnemiesLeaked = 0;
            _battleGoldRewards = 0;
            _battleManaRewards = 0;
            _battleFusions = 0;
            _battleHybridFusions = 0;
            _battleFusedDamageByDragon.Clear();
            OnWaveSummary?.Invoke(string.Empty);
        }

        public void BeginWave(int waveNumber)
        {
            _activeWave = waveNumber;
            ResetWaveCounters();
            OnWaveSummary?.Invoke(string.Empty);
        }

        public void RecordTowerPlaced(int manaCost)
        {
            _waveTowersPlaced++;
            _waveManaSpent += Mathf.Max(0, manaCost);
            _battleTowersPlaced++;
            _battleManaSpent += Mathf.Max(0, manaCost);
        }

        public void RecordManaRefunded(int amount)
        {
            _waveManaRefunded += Mathf.Max(0, amount);
            _battleManaRefunded += Mathf.Max(0, amount);
        }

        public void RecordSkillCast()
        {
            _waveSkillsCast++;
            _battleSkillsCast++;
        }

        public void RecordEnemyKilled()
        {
            _waveEnemiesKilled++;
            _battleEnemiesKilled++;
        }

        public void RecordEnemyLeaked()
        {
            _waveEnemiesLeaked++;
            _battleEnemiesLeaked++;
        }

        public void RecordReward(int gold, int mana)
        {
            _battleGoldRewards += Mathf.Max(0, gold);
            _battleManaRewards += Mathf.Max(0, mana);
        }

        public void RecordDamage(string dragonName, float amount)
        {
            RecordDamage(dragonName, amount, false);
        }

        public void RecordDamage(string dragonName, float amount, bool fused)
        {
            if (amount <= 0f) return;

            string key = string.IsNullOrWhiteSpace(dragonName) ? "Dragon" : dragonName;
            _waveDamageByDragon.TryGetValue(key, out float current);
            _waveDamageByDragon[key] = current + amount;
            _battleDamageByDragon.TryGetValue(key, out float battleCurrent);
            _battleDamageByDragon[key] = battleCurrent + amount;
            if (fused)
            {
                _battleFusedDamageByDragon.TryGetValue(key, out float fusedCurrent);
                _battleFusedDamageByDragon[key] = fusedCurrent + amount;
            }
        }

        public void RecordFusion(string resultName, bool hybrid)
        {
            _battleFusions++;
            if (hybrid)
                _battleHybridFusions++;
        }

        public string FinishWave(int waveNumber)
        {
            if (_activeWave != waveNumber)
                _activeWave = waveNumber;

            _latestSummary = BuildSummary(waveNumber);
            OnWaveSummary?.Invoke(_latestSummary);
            return _latestSummary;
        }

        public string BuildBattleSummary(bool victory, int wavesCleared, int totalWaves, int livesRemaining)
        {
            string topDragon = GetTopDamageDragon(_battleDamageByDragon, out float topDamage);
            string topFusedDragon = GetTopDamageDragon(_battleFusedDamageByDragon, out float topFusedDamage);
            var sb = new StringBuilder();
            sb.AppendLine(victory
                ? $"All {totalWaves} prototype waves cleared"
                : "The base fell before the prototype was cleared");
            sb.AppendLine($"Waves: {wavesCleared}/{totalWaves}  Lives: {livesRemaining}");
            sb.AppendLine($"Kills: {_battleEnemiesKilled}  Leaks: {_battleEnemiesLeaked}  Skills: {_battleSkillsCast}");
            sb.AppendLine($"Dragons placed: {_battleTowersPlaced}  Mana used: {_battleManaSpent}");
            sb.AppendLine($"Fusions: {_battleFusions}  Hybrids: {_battleHybridFusions}");
            sb.AppendLine($"Rewards: +{_battleGoldRewards}g  +{_battleManaRewards}MP  Refunds: +{_battleManaRefunded}MP");
            sb.AppendLine($"Best dragon: {topDragon}  {Mathf.RoundToInt(topDamage)} dmg");
            sb.Append($"Best fused: {topFusedDragon}  {Mathf.RoundToInt(topFusedDamage)} dmg");
            return sb.ToString();
        }

        private void ResetWaveCounters()
        {
            _waveDamageByDragon.Clear();
            _waveTowersPlaced = 0;
            _waveManaSpent = 0;
            _waveManaRefunded = 0;
            _waveSkillsCast = 0;
            _waveEnemiesKilled = 0;
            _waveEnemiesLeaked = 0;
        }

        private string BuildSummary(int waveNumber)
        {
            string topDragon = GetTopDamageDragon(_waveDamageByDragon, out float topDamage);

            var sb = new StringBuilder();
            sb.AppendLine($"Wave {waveNumber} Summary");
            sb.AppendLine($"Kills: {_waveEnemiesKilled}  Leaks: {_waveEnemiesLeaked}  Skills: {_waveSkillsCast}");
            sb.AppendLine($"Built: {_waveTowersPlaced}  Mana: -{_waveManaSpent} +{_waveManaRefunded}");
            sb.Append($"Top damage: {topDragon} {Mathf.RoundToInt(topDamage)}");
            return sb.ToString();
        }

        private static string GetTopDamageDragon(Dictionary<string, float> damageByDragon, out float topDamage)
        {
            string topDragon = "None";
            topDamage = 0f;
            foreach (KeyValuePair<string, float> pair in damageByDragon)
            {
                if (pair.Value <= topDamage) continue;
                topDragon = pair.Key;
                topDamage = pair.Value;
            }

            return topDragon;
        }
    }
}
