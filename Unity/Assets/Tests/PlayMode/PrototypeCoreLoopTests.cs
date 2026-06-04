using System.Collections;
using System.Reflection;
using DragonTD.Core;
using DragonTD.Dragons;
using DragonTD.TowerDefense;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DragonTD.Tests.PlayMode
{
    public class PrototypeCoreLoopTests
    {
        [TearDown]
        public void TearDown()
        {
            DestroyObjects<GameManager>();
            DestroyObjects<ResourceManager>();
            DestroyObjects<WaveManager>();
            DestroyObjects<BattleStatsTracker>();
            DestroyObjects<GameDirector>();
            DestroyObjects<DragonTower>();
            DestroyObjects<EnemyBase>();
            DestroyObjects<DamageIndicator>();
            DestroyObjects<SkillCastEffect>();
            DestroyObjects<DeathPopEffect>();

            ResetSingleton<GameManager>("Instance");
            ResetSingleton<ResourceManager>("Instance");
            ResetSingleton<WaveManager>("Instance");
            ResetSingleton<BattleStatsTracker>("Instance");
            ResetSingleton<GameDirector>("Instance");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator WaveProgression_ReachesVictoryAfterFiveConfiguredWaves()
        {
            CreateCoreManagers(CreateSingleEnemyWaves(5));
            GameManager.Instance.StartBattle();

            for (int i = 1; i <= 5; i++)
            {
                GameManager.Instance.StartNextWave();
                yield return null;
                WaveManager.Instance.OnEnemyDied();
                yield return new WaitUntil(() =>
                    GameManager.Instance.IsPlanningPhase ||
                    GameManager.Instance.State == GameState.Victory);
            }

            Assert.AreEqual(5, WaveManager.Instance.TotalWaves);
            Assert.AreEqual(5, GameManager.Instance.CurrentWave);
            Assert.AreEqual(GameState.Victory, GameManager.Instance.State);
        }

        [UnityTest]
        public IEnumerator PlanningState_BracketsActiveWaves()
        {
            CreateCoreManagers(CreateSingleEnemyWaves(2));
            GameManager.Instance.StartBattle();

            Assert.AreEqual(GameState.Planning, GameManager.Instance.State);
            Assert.IsTrue(GameManager.Instance.IsPlanningPhase);

            GameManager.Instance.StartNextWave();
            yield return null;

            Assert.AreEqual(GameState.Wave, GameManager.Instance.State);
            Assert.IsFalse(GameManager.Instance.IsPlanningPhase);

            WaveManager.Instance.OnEnemyDied();
            yield return null;

            Assert.AreEqual(GameState.Planning, GameManager.Instance.State);
            Assert.IsTrue(GameManager.Instance.IsPlanningPhase);
        }

        [Test]
        public void TowerUpgrade_SpendsExpectedGoldAndStopsAtLevelThree()
        {
            CreateCoreManagers(CreateEmptyWaves(1));
            GameManager.Instance.StartBattle();
            DragonTower tower = CreateTower("voltaris_001", "Voltaris", DragonElement.Lightning);

            Assert.AreEqual(1, tower.UpgradeLevel);
            Assert.AreEqual(PrototypeBalance.UpgradeBaseCost, tower.UpgradeCost);

            Assert.IsTrue(tower.TryUpgrade());
            Assert.AreEqual(2, tower.UpgradeLevel);
            Assert.AreEqual(PrototypeBalance.UpgradeBaseCost * 2, tower.UpgradeCost);

            Assert.IsTrue(tower.TryUpgrade());
            Assert.AreEqual(PrototypeBalance.MaxUpgradeLevel, tower.UpgradeLevel);
            Assert.IsTrue(tower.IsMaxUpgrade);
            Assert.AreEqual(PrototypeBalance.StartingGold - 105, ResourceManager.Instance.Gold);
            Assert.IsFalse(tower.TryUpgrade());
        }

        [UnityTest]
        public IEnumerator TowerUpgrade_IsPlanningOnly()
        {
            CreateCoreManagers(CreateSingleEnemyWaves(1));
            GameManager.Instance.StartBattle();
            DragonTower tower = CreateTower("voltaris_001", "Voltaris", DragonElement.Lightning);

            GameManager.Instance.StartNextWave();
            yield return null;

            Assert.AreEqual(GameState.Wave, GameManager.Instance.State);
            Assert.IsFalse(tower.TryUpgrade());
            Assert.AreEqual(1, tower.UpgradeLevel);
            Assert.AreEqual(PrototypeBalance.StartingGold, ResourceManager.Instance.Gold);
        }

        [UnityTest]
        public IEnumerator ActiveSkillCast_DamagesTargetAndRecordsSkillUse()
        {
            CreateCoreManagers(CreateEmptyWaves(1));
            GameManager.Instance.StartBattle();
            DragonTower tower = CreateTower("magmaclaw_003", "Magmaclaw", DragonElement.Fire);
            EnemyBase enemy = CreateEnemy(500f);

            Assert.IsFalse(tower.TryCastActiveSkill(enemy));
            Assert.AreEqual(1f, enemy.HpPercent);

            GameManager.Instance.SetState(GameState.Wave);
            Assert.IsTrue(tower.TryCastActiveSkill(enemy));
            yield return null;

            Assert.Less(enemy.HpPercent, 1f);
            string summary = BattleStatsTracker.Instance.FinishWave(1);
            StringAssert.Contains("Skills: 1", summary);
        }

        [UnityTest]
        public IEnumerator Fusion_ConsumesTargetAndCreatesHybridFusedTower()
        {
            CreateCoreManagers(CreateEmptyWaves(1));
            GameManager.Instance.StartBattle();
            ResourceManager.Instance.AddGold(1000);
            DragonTower primary = CreateTower("voltaris_001", "Voltaris", DragonElement.Lightning);
            DragonTower target = CreateTower("frostfang_002", "Frostfang", DragonElement.Ice, new Vector3(2f, 0f, 0f));
            UpgradeToLevelThree(primary);
            UpgradeToLevelThree(target);

            Assert.IsTrue(primary.TryMergeWith(target));
            yield return null;

            Assert.AreEqual(PrototypeBalance.FusedTowerLevel, primary.UpgradeLevel);
            Assert.IsTrue(primary.IsFused);
            Assert.AreEqual("Hybrid Fused", primary.TierLabel);
            Assert.IsTrue(target == null);
            string summary = BattleStatsTracker.Instance.BuildBattleSummary(true, 5, 5, 20);
            StringAssert.Contains("Fusions: 1", summary);
            StringAssert.Contains("Hybrids: 1", summary);
        }

        [UnityTest]
        public IEnumerator WaveClear_AppliesWaveRewards()
        {
            CreateCoreManagers(new[]
            {
                CreateWave(75, 45, CreateEnemyPrefab())
            });
            GameManager.Instance.StartBattle();

            GameManager.Instance.StartNextWave();
            yield return null;
            WaveManager.Instance.OnEnemyDied();
            yield return null;

            Assert.AreEqual(PrototypeBalance.StartingGold + 75, ResourceManager.Instance.Gold);
            Assert.AreEqual(PrototypeBalance.StartingMana + 45, ResourceManager.Instance.Mana);
            string summary = BattleStatsTracker.Instance.BuildBattleSummary(true, 1, 1, GameManager.Instance.Lives);
            StringAssert.Contains("Rewards: +75g  +45MP", summary);
        }

        private static void CreateCoreManagers(WaveData[] waves)
        {
            new GameObject("ResourceManager").AddComponent<ResourceManager>();
            new GameObject("GameManager").AddComponent<GameManager>();
            BattleStatsTracker.Ensure();
            var waveManager = new GameObject("WaveManager").AddComponent<WaveManager>();
            SetPrivateField(waveManager, "_waves", waves);
            var spawnPoint = new GameObject("TestSpawnPoint").transform;
            SetPrivateField(waveManager, "_spawnPoints", new[] { spawnPoint });
            SetPrivateField(waveManager, "_waypoints", new Transform[0]);
        }

        private static WaveData[] CreateEmptyWaves(int count)
        {
            var waves = new WaveData[count];
            for (int i = 0; i < count; i++)
                waves[i] = CreateWave(10 + i, 5 + i);
            return waves;
        }

        private static WaveData[] CreateSingleEnemyWaves(int count)
        {
            var waves = new WaveData[count];
            GameObject enemyPrefab = CreateEnemyPrefab();
            for (int i = 0; i < count; i++)
                waves[i] = CreateWave(10 + i, 5 + i, enemyPrefab);
            return waves;
        }

        private static WaveData CreateWave(int goldReward, int manaReward, GameObject enemyPrefab = null)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.EnemyGroups = enemyPrefab == null
                ? new EnemySpawnEntry[0]
                : new[] { new EnemySpawnEntry { EnemyPrefab = enemyPrefab, Count = 1, SpawnInterval = 0f } };
            wave.GoldReward = goldReward;
            wave.ManaReward = manaReward;
            return wave;
        }

        private static GameObject CreateEnemyPrefab()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.EnemyName = "Wave Target";
            data.MaxHp = 1f;
            data.MoveSpeed = 0f;
            data.Armor = 0f;

            var prefab = new GameObject("Wave Target Prefab");
            prefab.AddComponent<SpriteRenderer>();
            prefab.AddComponent<BoxCollider2D>();
            var enemy = prefab.AddComponent<EnemyBase>();
            SetPrivateField(enemy, "_data", data);
            return prefab;
        }

        private static DragonTower CreateTower(string dragonId, string displayName, DragonElement element)
        {
            return CreateTower(dragonId, displayName, element, Vector3.zero);
        }

        private static DragonTower CreateTower(string dragonId, string displayName, DragonElement element, Vector3 position)
        {
            var go = new GameObject(displayName);
            go.transform.position = position;
            go.AddComponent<SpriteRenderer>();
            var tower = go.AddComponent<DragonTower>();
            tower.Setup(new DragonInstance { Definition = CreateDragonDefinition(dragonId, displayName, element) });
            return tower;
        }

        private static DragonDefinition CreateDragonDefinition(string dragonId, string displayName, DragonElement element)
        {
            var normal = ScriptableObject.CreateInstance<SkillDefinition>();
            normal.skillId = dragonId + "_attack";
            normal.displayName = displayName + " Strike";
            normal.cooldown = 1f;
            normal.range = 4f;

            var active = ScriptableObject.CreateInstance<SkillDefinition>();
            active.skillId = dragonId + "_active";
            active.displayName = displayName + " Skill";
            active.cooldown = 1f;
            active.levelMultipliers = new[] { 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f };

            var definition = ScriptableObject.CreateInstance<DragonDefinition>();
            definition.dragonId = dragonId;
            definition.displayName = displayName;
            definition.element = element;
            definition.baseStats = new DragonBaseStats { attack = 120f, range = 4f };
            definition.skillSet = new DragonSkillSet { normalAttack = normal, activeSkill = active };
            definition.visualData = new DragonVisualData { primaryColor = Color.red };
            definition.manaCost = 80;
            return definition;
        }

        private static EnemyBase CreateEnemy(float hp)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.EnemyName = "Training Target";
            data.MaxHp = hp;
            data.MoveSpeed = 0f;
            data.Armor = 0f;

            var go = new GameObject("Training Target");
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<BoxCollider2D>();
            var enemy = go.AddComponent<EnemyBase>();
            SetPrivateField(enemy, "_data", data);
            enemy.Initialize(new Transform[0]);
            return enemy;
        }

        private static void UpgradeToLevelThree(DragonTower tower)
        {
            Assert.IsTrue(tower.TryUpgrade());
            Assert.IsTrue(tower.TryUpgrade());
            Assert.AreEqual(PrototypeBalance.MaxUpgradeLevel, tower.UpgradeLevel);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void ResetSingleton<T>(string propertyName)
        {
            PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
            property?.SetValue(null, null);
        }

        private static void DestroyObjects<T>() where T : Component
        {
            foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude))
            {
                if (component != null)
                    Object.DestroyImmediate(component.gameObject);
            }
        }
    }
}
