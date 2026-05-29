using UnityEngine;
using System;
using System.Collections.Generic;

namespace DragonTD.Core
{
    public class PlayerProgression
    {
        public const int MaxBuffLevel = 10;
        public const int StartingSummonTickets = 6;
        public const int StartingGems = 250;
        public const int ChestSlotCount = 4;

        public int Essence { get; private set; }
        public int Gold { get; private set; }
        public int Gems { get; private set; }
        public int SummonTickets { get; private set; }
        public int GachaPullsSinceLastEpic { get; private set; }
        public int GachaTotalPulls { get; private set; }
        public int DamageBuffLevel { get; private set; }
        public int AttackSpeedBuffLevel { get; private set; }
        public int StartingManaBuffLevel { get; private set; }
        public string CurrentStageId { get; private set; } = StageCatalog.DefaultStageId;
        public int HighestUnlockedStageIndex { get; private set; }
        public List<string> ClearedStageIds { get; private set; } = new List<string>();
        public List<StageStarSaveData> StageStars { get; private set; } = new List<StageStarSaveData>();
        public List<ChestSlotSaveData> ChestSlots { get; private set; } = new List<ChestSlotSaveData>();
        public List<EventClaimSaveData> EventClaims { get; private set; } = new List<EventClaimSaveData>();
        public List<DailyObjectiveSaveData> DailyObjectives { get; private set; } = new List<DailyObjectiveSaveData>();

        public float DamageMultiplier => 1f + DamageBuffLevel * 0.03f;
        public float AttackSpeedMultiplier => 1f + AttackSpeedBuffLevel * 0.025f;
        public int StartingManaBonus => StartingManaBuffLevel * 15;

        public event System.Action OnChanged;

        public void Load(PlayerProgressionSaveData saveData)
        {
            if (saveData == null)
            {
                Essence = 0;
                Gold = 0;
                Gems = StartingGems;
                SummonTickets = StartingSummonTickets;
                GachaPullsSinceLastEpic = 0;
                GachaTotalPulls = 0;
                DamageBuffLevel = 0;
                AttackSpeedBuffLevel = 0;
                StartingManaBuffLevel = 0;
                CurrentStageId = StageCatalog.DefaultStageId;
                HighestUnlockedStageIndex = 0;
                ClearedStageIds = new List<string>();
                StageStars = new List<StageStarSaveData>();
            }
            else
            {
                Essence = Mathf.Max(0, saveData.essence);
                Gold = Mathf.Max(0, saveData.gold);
                Gems = Mathf.Max(0, saveData.gems);
                SummonTickets = Mathf.Max(0, saveData.summonTickets);
                GachaPullsSinceLastEpic = Mathf.Max(0, saveData.gachaPullsSinceLastEpic);
                GachaTotalPulls         = Mathf.Max(0, saveData.gachaTotalPulls);
                DamageBuffLevel = Mathf.Clamp(saveData.damageBuffLevel, 0, MaxBuffLevel);
                AttackSpeedBuffLevel = Mathf.Clamp(saveData.attackSpeedBuffLevel, 0, MaxBuffLevel);
                StartingManaBuffLevel = Mathf.Clamp(saveData.startingManaBuffLevel, 0, MaxBuffLevel);
                CurrentStageId = string.IsNullOrWhiteSpace(saveData.currentStageId) ? StageCatalog.DefaultStageId : saveData.currentStageId;
                HighestUnlockedStageIndex = Mathf.Clamp(saveData.highestUnlockedStageIndex, 0, StageCatalog.Stages.Length - 1);
                ClearedStageIds = saveData.clearedStageIds != null ? new List<string>(saveData.clearedStageIds) : new List<string>();
                StageStars = saveData.stageStars != null ? new List<StageStarSaveData>(saveData.stageStars) : new List<StageStarSaveData>();
            }

            LoadChestSlots(saveData?.chestSlots);
            EventClaims = saveData?.eventClaims != null
                ? new List<EventClaimSaveData>(saveData.eventClaims)
                : new List<EventClaimSaveData>();
            DailyObjectives = saveData?.dailyObjectives != null
                ? new List<DailyObjectiveSaveData>(saveData.dailyObjectives)
                : new List<DailyObjectiveSaveData>();
            EnsureDailyObjectivesForToday();
            OnChanged?.Invoke();
        }

        public void WriteTo(PlayerProgressionSaveData saveData)
        {
            if (saveData == null) return;
            saveData.essence = Essence;
            saveData.gold = Gold;
            saveData.gems = Gems;
            saveData.summonTickets = SummonTickets;
            saveData.gachaPullsSinceLastEpic = GachaPullsSinceLastEpic;
            saveData.gachaTotalPulls         = GachaTotalPulls;
            saveData.damageBuffLevel = DamageBuffLevel;
            saveData.attackSpeedBuffLevel = AttackSpeedBuffLevel;
            saveData.startingManaBuffLevel = StartingManaBuffLevel;
            saveData.currentStageId = CurrentStageId;
            saveData.highestUnlockedStageIndex = HighestUnlockedStageIndex;
            saveData.clearedStageIds = new List<string>(ClearedStageIds);
            saveData.stageStars = new List<StageStarSaveData>(StageStars);
            saveData.chestSlots = new List<ChestSlotSaveData>(ChestSlots);
            saveData.eventClaims = new List<EventClaimSaveData>(EventClaims);
            saveData.dailyObjectives = new List<DailyObjectiveSaveData>(DailyObjectives);
        }

        public bool IsStageUnlocked(string stageId)
        {
            return StageCatalog.GetIndex(stageId) <= HighestUnlockedStageIndex;
        }

        public bool IsStageCleared(string stageId)
        {
            return !string.IsNullOrWhiteSpace(stageId) && ClearedStageIds.Contains(stageId);
        }

        public bool TrySelectStage(string stageId, out string message)
        {
            StageDefinition stage = StageCatalog.Get(stageId);
            if (!IsStageUnlocked(stage.stageId))
            {
                message = $"{stage.Title} is locked";
                return false;
            }

            CurrentStageId = stage.stageId;
            message = $"{stage.Title} selected";
            OnChanged?.Invoke();
            return true;
        }

        public void MarkStageCleared(string stageId)
        {
            StageDefinition stage = StageCatalog.Get(stageId);
            if (!ClearedStageIds.Contains(stage.stageId))
                ClearedStageIds.Add(stage.stageId);

            int nextIndex = StageCatalog.GetIndex(stage.stageId) + 1;
            if (nextIndex > HighestUnlockedStageIndex)
                HighestUnlockedStageIndex = Mathf.Clamp(nextIndex, 0, StageCatalog.Stages.Length - 1);
            CurrentStageId = stage.stageId;
            OnChanged?.Invoke();
        }

        public int GetBestStageStars(string stageId)
        {
            StageStarSaveData save = GetStageStars(stageId);
            return save != null ? Mathf.Clamp(save.bestStars, 0, 3) : 0;
        }

        public bool SetBestStageStars(string stageId, int stars)
        {
            StageDefinition stage = StageCatalog.Get(stageId);
            StageStarSaveData save = GetOrCreateStageStars(stage.stageId);
            int clamped = Mathf.Clamp(stars, 0, 3);
            if (clamped <= save.bestStars)
                return false;

            save.bestStars = clamped;
            OnChanged?.Invoke();
            return true;
        }

        public int GetBuffLevel(AccountBuffType buffType) => buffType switch
        {
            AccountBuffType.Damage => DamageBuffLevel,
            AccountBuffType.AttackSpeed => AttackSpeedBuffLevel,
            AccountBuffType.StartingMana => StartingManaBuffLevel,
            _ => 0
        };

        public int GetUpgradeCost(AccountBuffType buffType)
        {
            int level = GetBuffLevel(buffType);
            if (level >= MaxBuffLevel) return 0;
            return buffType switch
            {
                AccountBuffType.Damage => 35 + level * 20,
                AccountBuffType.AttackSpeed => 35 + level * 20,
                AccountBuffType.StartingMana => 25 + level * 15,
                _ => 999999
            };
        }

        public bool TryUpgrade(AccountBuffType buffType, out string message)
        {
            int level = GetBuffLevel(buffType);
            if (level >= MaxBuffLevel)
            {
                message = $"{Label(buffType)} is max level";
                return false;
            }

            int cost = GetUpgradeCost(buffType);
            if (Essence < cost)
            {
                message = $"Need {cost} essence for {Label(buffType)}";
                return false;
            }

            Essence -= cost;
            switch (buffType)
            {
                case AccountBuffType.Damage:
                    DamageBuffLevel++;
                    break;
                case AccountBuffType.AttackSpeed:
                    AttackSpeedBuffLevel++;
                    break;
                case AccountBuffType.StartingMana:
                    StartingManaBuffLevel++;
                    break;
            }

            message = $"{Label(buffType)} upgraded to Lv {GetBuffLevel(buffType)}";
            OnChanged?.Invoke();
            return true;
        }

        public int GrantBattleEssence(int wavesCleared, bool victory)
        {
            int amount = Mathf.Max(0, wavesCleared) * 8 + (victory ? 25 : 0);
            Essence += amount;
            OnChanged?.Invoke();
            return amount;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnChanged?.Invoke();
        }

        public bool TrySpendGold(int amount, out string message)
        {
            if (amount <= 0)
            {
                message = "No gold spent";
                return true;
            }

            if (Gold < amount)
            {
                message = $"Need {amount} gold";
                return false;
            }

            Gold -= amount;
            message = $"Spent {amount} gold";
            OnChanged?.Invoke();
            return true;
        }

        public bool TrySpendEssence(int amount, out string message)
        {
            if (amount <= 0)
            {
                message = "No essence spent";
                return true;
            }

            if (Essence < amount)
            {
                message = $"Need {amount} essence";
                return false;
            }

            Essence -= amount;
            message = $"Spent {amount} essence";
            OnChanged?.Invoke();
            return true;
        }

        public void AddEssence(int amount)
        {
            if (amount <= 0) return;
            Essence += amount;
            OnChanged?.Invoke();
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            Gems += amount;
            OnChanged?.Invoke();
        }

        public bool TrySpendGems(int amount, out string message)
        {
            if (amount <= 0)
            {
                message = "No gems spent";
                return true;
            }

            if (Gems < amount)
            {
                message = $"Need {amount} gems";
                return false;
            }

            Gems -= amount;
            message = $"Spent {amount} gems";
            OnChanged?.Invoke();
            return true;
        }

        public void GrantSummonTickets(int amount)
        {
            if (amount <= 0) return;
            SummonTickets += amount;
            OnChanged?.Invoke();
        }

        public void SyncGachaPity(int pullsSinceLastEpic, int totalPulls)
        {
            GachaPullsSinceLastEpic = Mathf.Max(0, pullsSinceLastEpic);
            GachaTotalPulls         = Mathf.Max(0, totalPulls);
            OnChanged?.Invoke();
        }

        public bool TrySpendSummonTicket(out string message)
        {
            if (SummonTickets <= 0)
            {
                message = "Need a Dragon Summon Ticket";
                return false;
            }

            SummonTickets--;
            message = "Dragon Summon Ticket spent";
            OnChanged?.Invoke();
            return true;
        }

        public bool TryClaimEventReward(PrototypeEventDefinition eventDefinition, out string message)
        {
            if (eventDefinition == null)
            {
                message = "No event selected";
                return false;
            }

            if (!eventDefinition.claimable)
            {
                message = eventDefinition.locked
                    ? $"{eventDefinition.displayName} is locked"
                    : $"{eventDefinition.displayName} is preview only";
                return false;
            }

            EventClaimSaveData claim = GetOrCreateEventClaim(eventDefinition.eventId);
            if (IsClaimedToday(claim))
            {
                message = $"{eventDefinition.displayName} already claimed today";
                return false;
            }

            Gold += eventDefinition.goldReward;
            Essence += eventDefinition.essenceReward;
            Gems += eventDefinition.gemReward;
            claim.lastClaimUtcTicks = DateTime.UtcNow.Ticks;
            claim.claimCount++;
            message = $"Claimed {eventDefinition.displayName}: {eventDefinition.RewardText}";
            OnChanged?.Invoke();
            return true;
        }

        public void RecordDailyObjectiveProgress(DailyObjectiveType objectiveType, int amount = 1)
        {
            EnsureDailyObjectivesForToday();
            foreach (DailyObjectiveDefinition definition in DailyObjectiveCatalog.Objectives)
            {
                if (definition.objectiveType != objectiveType)
                    continue;

                DailyObjectiveSaveData save = GetOrCreateDailyObjective(definition.objectiveId);
                save.progress = Mathf.Min(definition.targetCount, save.progress + Mathf.Max(1, amount));
            }
            OnChanged?.Invoke();
        }

        public bool TryClaimDailyObjective(string objectiveId, out string message)
        {
            EnsureDailyObjectivesForToday();
            DailyObjectiveDefinition definition = DailyObjectiveCatalog.Get(objectiveId);
            if (definition == null)
            {
                message = "Objective unavailable";
                return false;
            }

            DailyObjectiveSaveData save = GetOrCreateDailyObjective(objectiveId);
            if (save.claimed)
            {
                message = $"{definition.displayName} already claimed";
                return false;
            }

            if (save.progress < definition.targetCount)
            {
                message = $"{definition.displayName}: {save.progress}/{definition.targetCount}";
                return false;
            }

            Gold += definition.goldReward;
            Essence += definition.essenceReward;
            Gems += definition.gemReward;
            save.claimed = true;
            message = $"Claimed {definition.displayName}: {definition.RewardText}";
            OnChanged?.Invoke();
            return true;
        }

        public string BuildDailyObjectiveStatus(DailyObjectiveDefinition definition)
        {
            EnsureDailyObjectivesForToday();
            if (definition == null) return "Unavailable";
            DailyObjectiveSaveData save = GetOrCreateDailyObjective(definition.objectiveId);
            if (save.claimed) return "Claimed";
            if (save.progress >= definition.targetCount) return "Ready";
            return $"{save.progress}/{definition.targetCount}";
        }

        public string BuildDailyObjectiveSummary()
        {
            EnsureDailyObjectivesForToday();
            var sb = new System.Text.StringBuilder();
            foreach (DailyObjectiveDefinition definition in DailyObjectiveCatalog.Objectives)
                sb.AppendLine($"{definition.displayName}: {BuildDailyObjectiveStatus(definition)}");
            return sb.ToString();
        }

        public bool IsEventClaimedToday(string eventId)
        {
            return IsClaimedToday(GetEventClaim(eventId));
        }

        public string BuildEventStatus(PrototypeEventDefinition eventDefinition)
        {
            if (eventDefinition == null) return "Unavailable";
            if (eventDefinition.locked) return "Locked";
            if (!eventDefinition.claimable) return "Preview";
            return IsEventClaimedToday(eventDefinition.eventId) ? "Claimed today" : "Ready to claim";
        }

        public bool TryStartChestUnlock(int slotIndex, out string message)
        {
            ChestSlotSaveData slot = GetChestSlot(slotIndex);
            if (slot == null)
            {
                message = "No chest slot";
                return false;
            }

            if (!HasChest(slot))
            {
                message = "Chest slot is empty";
                return false;
            }

            if (IsChestUnlocking(slot))
            {
                message = $"{slot.rarity} chest is already unlocking";
                return false;
            }

            if (IsChestReady(slot))
            {
                message = $"{slot.rarity} chest is ready";
                return false;
            }

            slot.unlockCompleteUtcTicks = DateTime.UtcNow.AddHours(GetChestUnlockHours(slot.rarity)).Ticks;
            message = $"{slot.rarity} chest started";
            OnChanged?.Invoke();
            return true;
        }

        public bool TryOpenChest(int slotIndex, out string message)
        {
            bool opened = TryOpenChest(slotIndex, out ChestRewardResult reward, out message);
            return opened;
        }

        public bool TryOpenChest(int slotIndex, out ChestRewardResult reward, out string message)
        {
            ChestSlotSaveData slot = GetChestSlot(slotIndex);
            reward = null;
            if (slot == null)
            {
                message = "No chest slot";
                return false;
            }

            if (!HasChest(slot))
            {
                message = "Chest slot is empty";
                return false;
            }

            if (!IsChestReady(slot))
            {
                message = "Chest is not ready";
                return false;
            }

            int goldReward = GetChestGoldReward(slot.rarity);
            int gemReward = GetChestGemReward(slot.rarity);
            Gold += goldReward;
            Gems += gemReward;
            reward = new ChestRewardResult
            {
                slotIndex = slotIndex,
                rarity = slot.rarity,
                gold = goldReward,
                gems = gemReward,
                summary = $"Opened {slot.rarity} chest: +{goldReward} gold, +{gemReward} gems"
            };
            message = reward.summary;
            ResetChest(slot, slotIndex);
            OnChanged?.Invoke();
            return true;
        }

        public bool TrySpeedUpChestUnlock(int slotIndex, out string message)
        {
            ChestSlotSaveData slot = GetChestSlot(slotIndex);
            if (slot == null)
            {
                message = "No chest slot";
                return false;
            }

            if (!HasChest(slot))
            {
                message = "Chest slot is empty";
                return false;
            }

            if (IsChestReady(slot))
            {
                message = $"{slot.rarity} chest is already ready";
                return false;
            }

            if (!IsChestUnlocking(slot))
            {
                message = "Start unlocking this chest first";
                return false;
            }

            int cost = GetChestSpeedUpGemCost(slotIndex);
            if (!TrySpendGems(cost, out message))
                return false;

            slot.unlockCompleteUtcTicks = DateTime.UtcNow.Ticks;
            message = $"{slot.rarity} chest sped up for {cost} gems";
            OnChanged?.Invoke();
            return true;
        }

        public bool TryAwardBattleChest(string rarity, out int slotIndex, out string message)
        {
            EnsureChestSlots();
            slotIndex = FindEmptyChestSlot();
            if (slotIndex < 0)
            {
                message = "Chest slots full";
                return false;
            }

            ChestSlotSaveData slot = ChestSlots[slotIndex];
            slot.rarity = NormalizeChestRarity(rarity);
            slot.unlockCompleteUtcTicks = 0;
            slot.claimed = false;
            message = $"Found {slot.rarity} chest in slot {slotIndex + 1}";
            OnChanged?.Invoke();
            return true;
        }

        public string BuildChestSlotLabel(int slotIndex)
        {
            ChestSlotSaveData slot = GetChestSlot(slotIndex);
            if (slot == null)
                return "Empty";
            if (!HasChest(slot))
                return "Empty Slot\nWin a battle\nfor a chest";

            string action = IsChestReady(slot) ? "Tap to open" :
                IsChestUnlocking(slot) ? $"Speed up: {GetChestSpeedUpGemCost(slotIndex)} gems" : "Tap to unlock";

            return $"{slot.rarity} Chest\n{ChestStatus(slot)}\n{GetChestRewardPreview(slot.rarity)}\n{action}";
        }

        public bool IsChestSlotReady(int slotIndex)
        {
            return IsChestReady(GetChestSlot(slotIndex));
        }

        public bool IsChestSlotUnlocking(int slotIndex)
        {
            return IsChestUnlocking(GetChestSlot(slotIndex));
        }

        public string GetChestSlotRarity(int slotIndex)
        {
            return GetChestSlot(slotIndex)?.rarity ?? "Empty";
        }

        public string GetChestSlotRewardPreview(int slotIndex)
        {
            ChestSlotSaveData slot = GetChestSlot(slotIndex);
            return slot == null ? string.Empty : GetChestRewardPreview(slot.rarity);
        }

        public int GetChestSpeedUpGemCost(int slotIndex)
        {
            ChestSlotSaveData slot = GetChestSlot(slotIndex);
            if (!IsChestUnlocking(slot))
                return 0;

            TimeSpan remaining = TimeSpan.FromTicks(slot.unlockCompleteUtcTicks - DateTime.UtcNow.Ticks);
            return Mathf.Max(1, Mathf.CeilToInt((float)remaining.TotalMinutes / 10f));
        }

        public string BuildChestSummary()
        {
            EnsureChestSlots();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ChestSlots.Count; i++)
            {
                ChestSlotSaveData slot = ChestSlots[i];
                sb.AppendLine(HasChest(slot)
                    ? $"{i + 1}. {slot.rarity} - {ChestStatus(slot)}"
                    : $"{i + 1}. Empty");
            }
            return sb.ToString();
        }

        public string BuildSummary()
        {
            return $"Essence: {Essence} (buff upgrade currency)\n" +
                   $"Gold: {Gold}\n" +
                   $"Gems: {Gems}\n" +
                   $"Dragon Summon Tickets: {SummonTickets}\n" +
                   $"Damage Lv {DamageBuffLevel}: +{DamageBuffLevel * 3}%\n" +
                   $"Attack Speed Lv {AttackSpeedBuffLevel}: +{AttackSpeedBuffLevel * 2.5f:0.#}%\n" +
                   $"Starting Mana Lv {StartingManaBuffLevel}: +{StartingManaBonus} MP";
        }

        public static string Label(AccountBuffType buffType) => buffType switch
        {
            AccountBuffType.Damage => "Damage Training",
            AccountBuffType.AttackSpeed => "Attack Drill",
            AccountBuffType.StartingMana => "Mana Reserve",
            _ => "Buff"
        };

        private void LoadChestSlots(List<ChestSlotSaveData> savedSlots)
        {
            ChestSlots = savedSlots != null ? new List<ChestSlotSaveData>(savedSlots) : new List<ChestSlotSaveData>();
            EnsureChestSlots();
        }

        private EventClaimSaveData GetEventClaim(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)) return null;
            foreach (EventClaimSaveData claim in EventClaims)
            {
                if (claim != null && claim.eventId == eventId)
                    return claim;
            }
            return null;
        }

        private EventClaimSaveData GetOrCreateEventClaim(string eventId)
        {
            EventClaimSaveData claim = GetEventClaim(eventId);
            if (claim != null) return claim;

            claim = new EventClaimSaveData { eventId = eventId };
            EventClaims.Add(claim);
            return claim;
        }

        private StageStarSaveData GetStageStars(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId)) return null;
            foreach (StageStarSaveData save in StageStars)
            {
                if (save != null && save.stageId == stageId)
                    return save;
            }
            return null;
        }

        private StageStarSaveData GetOrCreateStageStars(string stageId)
        {
            StageStarSaveData save = GetStageStars(stageId);
            if (save != null) return save;

            save = new StageStarSaveData { stageId = stageId, bestStars = 0 };
            StageStars.Add(save);
            return save;
        }

        private void EnsureDailyObjectivesForToday()
        {
            long todayTicks = DateTime.UtcNow.Date.Ticks;
            foreach (DailyObjectiveDefinition definition in DailyObjectiveCatalog.Objectives)
            {
                DailyObjectiveSaveData save = GetDailyObjective(definition.objectiveId);
                if (save == null)
                {
                    DailyObjectives.Add(new DailyObjectiveSaveData
                    {
                        objectiveId = definition.objectiveId,
                        dateUtcTicks = todayTicks,
                        progress = 0,
                        claimed = false
                    });
                }
                else if (save.dateUtcTicks != todayTicks)
                {
                    save.dateUtcTicks = todayTicks;
                    save.progress = 0;
                    save.claimed = false;
                }
            }
        }

        private DailyObjectiveSaveData GetDailyObjective(string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(objectiveId)) return null;
            foreach (DailyObjectiveSaveData objective in DailyObjectives)
            {
                if (objective != null && objective.objectiveId == objectiveId)
                    return objective;
            }
            return null;
        }

        private DailyObjectiveSaveData GetOrCreateDailyObjective(string objectiveId)
        {
            DailyObjectiveSaveData objective = GetDailyObjective(objectiveId);
            if (objective != null) return objective;

            objective = new DailyObjectiveSaveData
            {
                objectiveId = objectiveId,
                dateUtcTicks = DateTime.UtcNow.Date.Ticks
            };
            DailyObjectives.Add(objective);
            return objective;
        }

        private static bool IsClaimedToday(EventClaimSaveData claim)
        {
            if (claim == null || claim.lastClaimUtcTicks <= 0) return false;
            return new DateTime(claim.lastClaimUtcTicks, DateTimeKind.Utc).Date == DateTime.UtcNow.Date;
        }

        private void EnsureChestSlots()
        {
            for (int i = ChestSlots.Count; i < ChestSlotCount; i++)
            {
                ChestSlots.Add(new ChestSlotSaveData
                {
                    rarity = string.Empty,
                    unlockCompleteUtcTicks = 0,
                    claimed = true
                });
            }

            if (ChestSlots.Count > ChestSlotCount)
                ChestSlots.RemoveRange(ChestSlotCount, ChestSlots.Count - ChestSlotCount);
        }

        private ChestSlotSaveData GetChestSlot(int slotIndex)
        {
            EnsureChestSlots();
            return slotIndex >= 0 && slotIndex < ChestSlots.Count ? ChestSlots[slotIndex] : null;
        }

        private static bool IsChestReady(ChestSlotSaveData slot) =>
            HasChest(slot) && slot.unlockCompleteUtcTicks > 0 && DateTime.UtcNow.Ticks >= slot.unlockCompleteUtcTicks;

        private static bool IsChestUnlocking(ChestSlotSaveData slot) =>
            HasChest(slot) && slot.unlockCompleteUtcTicks > DateTime.UtcNow.Ticks;

        private static string ChestStatus(ChestSlotSaveData slot)
        {
            if (slot == null) return "Empty";
            if (!HasChest(slot)) return "Empty";
            if (slot.unlockCompleteUtcTicks <= 0)
                return $"Locked ({GetChestUnlockHours(slot.rarity)}h)";
            long ticksRemaining = slot.unlockCompleteUtcTicks - DateTime.UtcNow.Ticks;
            if (ticksRemaining <= 0)
                return "Ready to open";
            TimeSpan remaining = TimeSpan.FromTicks(ticksRemaining);
            return remaining.TotalHours >= 1
                ? $"{Mathf.CeilToInt((float)remaining.TotalHours)}h left"
                : $"{Mathf.CeilToInt((float)remaining.TotalMinutes)}m left";
        }

        private static int GetChestUnlockHours(string rarity) => rarity switch
        {
            "Common" => 3,
            "Rare" => 8,
            "Epic" => 12,
            "Legendary" => 24,
            _ => 3
        };

        private static int GetChestGoldReward(string rarity) => rarity switch
        {
            "Common" => 120,
            "Rare" => 300,
            "Epic" => 650,
            "Legendary" => 1500,
            _ => 100
        };

        private static int GetChestGemReward(string rarity) => rarity switch
        {
            "Common" => 5,
            "Rare" => 15,
            "Epic" => 40,
            "Legendary" => 120,
            _ => 5
        };

        private static string GetChestRewardPreview(string rarity)
        {
            return $"+{GetChestGoldReward(rarity)} gold, +{GetChestGemReward(rarity)} gems";
        }

        private static void ResetChest(ChestSlotSaveData slot, int slotIndex)
        {
            slot.rarity = string.Empty;
            slot.unlockCompleteUtcTicks = 0;
            slot.claimed = true;
        }

        private int FindEmptyChestSlot()
        {
            for (int i = 0; i < ChestSlots.Count; i++)
            {
                if (!HasChest(ChestSlots[i]))
                    return i;
            }

            return -1;
        }

        private static bool HasChest(ChestSlotSaveData slot)
        {
            return slot != null && !slot.claimed && !string.IsNullOrWhiteSpace(slot.rarity);
        }

        private static string NormalizeChestRarity(string rarity) => rarity switch
        {
            "Common" => "Common",
            "Rare" => "Rare",
            "Epic" => "Epic",
            "Legendary" => "Legendary",
            _ => "Common"
        };
    }
}
