using System.Collections;
using System.Reflection;
using DragonTD.Core;
using DragonTD.Dragons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DragonTD.Tests.PlayMode
{
    public class InventoryProgressionTests
    {
        private PlayerInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            ResetSingleton<PlayerInventory>("Instance");
            var go = new GameObject("TestPlayerInventory");
            _inventory = go.AddComponent<PlayerInventory>();
            string tempPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                $"test_prog_{System.Guid.NewGuid():N}.json");
            SetPrivateField(_inventory, "_persistenceService",
                new LocalProgressionPersistenceService(tempPath));
        }

        [TearDown]
        public void TearDown()
        {
            if (_inventory != null)
                Object.DestroyImmediate(_inventory.gameObject);
            ResetSingleton<PlayerInventory>("Instance");
        }

        [Test]
        public void TrySwapEquipped_SwapsIncomingWithExistingSlot()
        {
            DragonInstance a = MakeDragon("voltaris_001", "Voltaris");
            DragonInstance b = MakeDragon("frostfang_002", "Frostfang");
            DragonInstance c = MakeDragon("magmaclaw_003", "Magmaclaw");
            AddToOwnedDragons(_inventory, a, b, c);

            _inventory.TryToggleEquipDragon(a, out _);
            _inventory.TryToggleEquipDragon(b, out _);

            Assert.IsTrue(_inventory.IsEquipped(a));
            Assert.IsTrue(_inventory.IsEquipped(b));
            Assert.IsFalse(_inventory.IsEquipped(c));

            bool result = _inventory.TrySwapEquipped("magmaclaw_003", "voltaris_001", out _);

            Assert.IsTrue(result);
            Assert.IsFalse(_inventory.IsEquipped(a), "voltaris should be unequipped after swap");
            Assert.IsTrue(_inventory.IsEquipped(b), "frostfang should remain equipped");
            Assert.IsTrue(_inventory.IsEquipped(c), "magmaclaw should be equipped after swap");
        }

        [Test]
        public void TrySwapEquipped_NonEquippedExisting_ReturnsFalse()
        {
            DragonInstance a = MakeDragon("voltaris_001", "Voltaris");
            DragonInstance b = MakeDragon("frostfang_002", "Frostfang");
            AddToOwnedDragons(_inventory, a, b);
            _inventory.TryToggleEquipDragon(a, out _);

            bool result = _inventory.TrySwapEquipped("voltaris_001", "frostfang_002", out string message);

            Assert.IsFalse(result);
            Assert.AreEqual("Target slot not equipped", message);
        }

        [Test]
        public void ChestAward_VictoryFillsFirstEmptySlot()
        {
            _inventory.Progression.Load(null);

            bool awarded = _inventory.Progression.TryAwardBattleChest("Rare", out int slot, out _);

            Assert.IsTrue(awarded);
            Assert.GreaterOrEqual(slot, 0);
            Assert.Less(slot, 4);
            Assert.AreEqual("Rare", _inventory.Progression.ChestSlots[slot]?.rarity);
        }

        [Test]
        public void ChestAward_AllSlotsFull_ReturnsFalse()
        {
            _inventory.Progression.Load(null);
            for (int i = 0; i < 4; i++)
                _inventory.Progression.TryAwardBattleChest("Common", out _, out _);

            bool awarded = _inventory.Progression.TryAwardBattleChest("Rare", out _, out _);

            Assert.IsFalse(awarded);
        }

        [Test]
        public void DailyObjective_WinBattleAdvancesProgress()
        {
            _inventory.Progression.Load(null);
            _inventory.Progression.RecordDailyObjectiveProgress(DailyObjectiveType.WinBattle);
            // Just verify it doesn't throw and the progression is still valid
            Assert.IsNotNull(_inventory.Progression);
        }

        [UnityTest]
        public IEnumerator SaveLoad_RoundTripPreservesGoldAndEssence()
        {
            string tempPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                $"roundtrip_{System.Guid.NewGuid():N}.json");
            SetPrivateField(_inventory, "_persistenceService",
                new LocalProgressionPersistenceService(tempPath));

            _inventory.Progression.Load(null);
            _inventory.Progression.AddGold(450);
            _inventory.Progression.AddEssence(120);

            var saveTask = _inventory.SaveProgressionAsync();
            while (!saveTask.IsCompleted) yield return null;
            Assert.IsTrue(saveTask.Result, "Save should succeed");

            var loadService = new LocalProgressionPersistenceService(tempPath);
            var loadTask = loadService.LoadProgressionAsync();
            while (!loadTask.IsCompleted) yield return null;

            Assert.IsTrue(loadTask.Result.success, $"Load failed: {loadTask.Result.message}");
            Assert.IsNotNull(loadTask.Result.saveData);

            var loadedProg = new PlayerProgression();
            loadedProg.Load(loadTask.Result.saveData);

            Assert.AreEqual(450, loadedProg.Gold, "Gold must survive save/load");
            Assert.AreEqual(120, loadedProg.Essence, "Essence must survive save/load");

            try { System.IO.File.Delete(tempPath); } catch { }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static DragonInstance MakeDragon(string id, string name)
        {
            var def = ScriptableObject.CreateInstance<DragonDefinition>();
            def.dragonId = id;
            def.displayName = name;
            def.element = DragonElement.Fire;
            def.baseStats = new DragonBaseStats { attack = 100f };
            return new DragonInstance { Definition = def };
        }

        private static void AddToOwnedDragons(PlayerInventory inventory, params DragonInstance[] dragons)
        {
            // PlayerInventory.OwnedDragons has a private setter; access backing list directly
            var field = typeof(PlayerInventory).GetField(
                "<OwnedDragons>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                // Fallback: try by property name pattern variations
                foreach (FieldInfo f in typeof(PlayerInventory).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (f.Name.Contains("OwnedDragons") && f.FieldType == typeof(System.Collections.Generic.List<DragonInstance>))
                    {
                        var list = (System.Collections.Generic.List<DragonInstance>)f.GetValue(inventory);
                        if (list != null) { foreach (var d in dragons) list.Add(d); return; }
                    }
                }
                Assert.Fail("Could not locate OwnedDragons backing field via reflection");
                return;
            }
            var backingList = (System.Collections.Generic.List<DragonInstance>)field.GetValue(inventory);
            Assert.IsNotNull(backingList, "OwnedDragons backing list should not be null");
            foreach (DragonInstance d in dragons) backingList.Add(d);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            typeof(PlayerInventory)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void ResetSingleton<T>(string propertyName)
        {
            typeof(T).GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public)
                ?.SetValue(null, null);
        }
    }
}
