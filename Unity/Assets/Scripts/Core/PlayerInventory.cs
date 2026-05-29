using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DragonTD.Dragons;

namespace DragonTD.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        public const int MaxEquippedDragons = 6;

        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private DragonDefinition[] _starterDragons;

        public List<DragonInstance> OwnedDragons { get; private set; } = new List<DragonInstance>();
        public List<string> EquippedDragonIds { get; private set; } = new List<string>();
        public PlayerProgression Progression { get; private set; } = new PlayerProgression();
        public string LastBattleRewardSummary { get; private set; } = string.Empty;
        public string LastSummonSummary { get; private set; } = string.Empty;
        public BattleRewardResult LastBattleRewardResult { get; private set; }
        public string SyncStatus { get; private set; } = "Local";

        public event System.Action OnInventoryChanged;
        public event System.Action<string> OnBattleRewardsGranted;
        public event System.Action<BattleRewardResult> OnBattleRewardResultGranted;
        public event System.Action OnProgressionChanged;
        public event System.Action<string> OnSyncStatusChanged;
        public event System.Action OnLoadoutChanged;
        public event System.Action<string> OnSummonResult;

        private string SavePath => Path.Combine(Application.persistentDataPath, "dragon_dominion_progression.json");
        private IProgressionPersistenceService _persistenceService;
        private IStorePurchaseService _purchaseService;
        private AuthSessionData _authSession;

        private void Awake()
        {
            if (Instance != null)
            {
                Instance.AbsorbStarterDragons(_starterDragons);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Progression.OnChanged += HandleProgressionChanged;
            _persistenceService = new LocalProgressionPersistenceService(SavePath);
            _purchaseService = CreatePurchaseService(new LocalIapReceiptValidator());
            SetSyncStatus(_persistenceService.ModeLabel);
        }

        private async void Start()
        {
            if (!await LoadProgressionAsync())
                Progression.Load(null);
        }

        public void AddDragon(DragonDefinition def)
        {
            if (def == null) return;
            var dragon = new DragonInstance { Definition = def };
            OwnedDragons.Add(dragon);
            EquipDefaultIfSlotAvailable(dragon);
            SaveProgressionAsync();
            OnInventoryChanged?.Invoke();
            OnLoadoutChanged?.Invoke();
        }

        public bool TrySummonDragon(out string message)
        {
            DragonDefinition summoned = PickSummonDragon();
            if (summoned == null)
            {
                message = "No new dragons are available to summon";
                LastSummonSummary = message;
                OnSummonResult?.Invoke(message);
                return false;
            }

            if (!Progression.TrySpendSummonTicket(out message))
            {
                LastSummonSummary = message;
                OnSummonResult?.Invoke(message);
                return false;
            }

            AddDragon(summoned);
            message = $"Summoned {summoned.displayName}";
            LastSummonSummary = message;
            Progression.RecordDailyObjectiveProgress(DailyObjectiveType.SummonDragon);
            OnSummonResult?.Invoke(message);
            OnProgressionChanged?.Invoke();
            return true;
        }

        public List<DragonInstance> GetBattleDragons()
        {
            EnsureValidLoadout();
            var battleDragons = new List<DragonInstance>();
            foreach (string dragonId in EquippedDragonIds)
            {
                DragonInstance dragon = FindOwnedDragon(dragonId);
                if (dragon != null)
                    battleDragons.Add(dragon);
            }
            return battleDragons;
        }

        public bool IsEquipped(DragonInstance dragon)
        {
            string dragonId = dragon?.Definition?.dragonId;
            return !string.IsNullOrWhiteSpace(dragonId) && EquippedDragonIds.Contains(dragonId);
        }

        public DragonInstance FindOwnedDragonById(string dragonId) => FindOwnedDragon(dragonId);

        public bool TrySwapEquipped(string incomingId, string existingId, out string message)
        {
            if (string.IsNullOrWhiteSpace(incomingId) || string.IsNullOrWhiteSpace(existingId))
            {
                message = "Invalid dragon IDs";
                return false;
            }
            EnsureValidLoadout();
            if (!EquippedDragonIds.Contains(existingId))
            {
                message = "Target slot not equipped";
                return false;
            }
            int incomingIndex = EquippedDragonIds.IndexOf(incomingId);
            int existingIndex = EquippedDragonIds.IndexOf(existingId);
            if (incomingIndex >= 0)
            {
                // Both already equipped — swap positions
                EquippedDragonIds[existingIndex] = incomingId;
                EquippedDragonIds[incomingIndex] = existingId;
            }
            else
            {
                EquippedDragonIds.Remove(existingId);
                EquippedDragonIds.Add(incomingId);
            }
            message = $"Swapped to {incomingId}";
            SaveProgressionAsync();
            OnLoadoutChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryToggleEquipDragon(DragonInstance dragon, out string message)
        {
            string dragonId = dragon?.Definition?.dragonId;
            if (string.IsNullOrWhiteSpace(dragonId))
            {
                message = "No dragon selected";
                return false;
            }

            EnsureValidLoadout();
            if (EquippedDragonIds.Contains(dragonId))
            {
                EquippedDragonIds.Remove(dragonId);
                message = $"{dragon.Definition.displayName} unequipped";
            }
            else
            {
                if (EquippedDragonIds.Count >= MaxEquippedDragons)
                {
                    message = $"Battle loadout is full ({MaxEquippedDragons}/{MaxEquippedDragons})";
                    return false;
                }

                EquippedDragonIds.Add(dragonId);
                message = $"{dragon.Definition.displayName} equipped";
            }

            SaveProgressionAsync();
            OnLoadoutChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryApplyLoadoutPreset(LoadoutPresetType preset, out string message)
        {
            EnsureValidLoadout();
            var selected = new List<DragonInstance>();
            DragonRoleTag[] priorities = GetPresetPriorities(preset);
            foreach (DragonRoleTag role in priorities)
                AddBestDragonForRole(selected, role);

            foreach (DragonInstance dragon in OwnedDragons)
            {
                if (selected.Count >= MaxEquippedDragons)
                    break;
                if (dragon?.Definition != null && !selected.Contains(dragon))
                    selected.Add(dragon);
            }

            if (selected.Count == 0)
            {
                message = "No owned dragons available";
                return false;
            }

            EquippedDragonIds.Clear();
            foreach (DragonInstance dragon in selected)
            {
                if (EquippedDragonIds.Count >= MaxEquippedDragons)
                    break;
                string dragonId = dragon?.Definition?.dragonId;
                if (!string.IsNullOrWhiteSpace(dragonId) && !EquippedDragonIds.Contains(dragonId))
                    EquippedDragonIds.Add(dragonId);
            }

            SaveProgressionAsync();
            OnLoadoutChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            message = $"{PresetLabel(preset)} preset equipped ({EquippedDragonIds.Count}/{MaxEquippedDragons})";
            return true;
        }

        public string BuildLoadoutCoverageWarning(StageDefinition stage)
        {
            if (stage?.recommendedRoles == null || stage.recommendedRoles.Length == 0)
                return string.Empty;

            List<DragonInstance> loadout = GetBattleDragons();
            var missing = new List<DragonRoleTag>();
            foreach (DragonRoleTag role in stage.recommendedRoles)
            {
                if (!DragonRoleUtility.LoadoutHasRole(loadout, role))
                    missing.Add(role);
            }

            return missing.Count == 0
                ? string.Empty
                : $"Loadout warning: missing {DragonRoleUtility.BuildRoleList(missing)}";
        }

        public string BuildLoadoutCoverageReport()
        {
            return DragonRoleUtility.BuildCoverageReport(GetBattleDragons());
        }

        public BattleRewardResult GrantBattleCompletionRewards(int wavesCleared, bool victory)
        {
            var result = new BattleRewardResult
            {
                victory = victory,
                wavesCleared = Mathf.Max(0, wavesCleared)
            };

            List<DragonInstance> battleDragons = GetBattleDragons();
            if (battleDragons.Count == 0)
            {
                StageDefinition noDragonStage = StageCatalog.Get(GameManager.Instance?.CurrentStageId);
                int noDragonBaseEssence = Progression.GrantBattleEssence(wavesCleared, victory);
                int noDragonBonusEssence = Mathf.RoundToInt(noDragonBaseEssence * (Mathf.Max(1f, noDragonStage.rewardMultiplier) - 1f));
                if (noDragonBonusEssence > 0)
                    Progression.AddEssence(noDragonBonusEssence);
                int noDragonEssence = noDragonBaseEssence + noDragonBonusEssence;
                int noDragonTickets = victory ? 1 : 0;
                if (noDragonTickets > 0)
                    Progression.GrantSummonTickets(noDragonTickets);
                AwardBattleChest(result, victory);
                if (victory)
                {
                    Progression.RecordDailyObjectiveProgress(DailyObjectiveType.WinBattle);
                }
                ApplyStageResult(result, victory);
                string noDragonTicketText = noDragonTickets > 0 ? $", +{noDragonTickets} summon ticket" : string.Empty;
                string noDragonChestText = BuildChestRewardSummary(result);
                string noDragonStageText = BuildStageRewardSummary(result);
                LastBattleRewardSummary = $"Progression: no equipped dragons received bond XP, +{noDragonEssence} essence{noDragonTicketText}{noDragonChestText}{noDragonStageText}";
                result.summary = LastBattleRewardSummary;
                LastBattleRewardResult = result;
                OnBattleRewardsGranted?.Invoke(LastBattleRewardSummary);
                OnBattleRewardResultGranted?.Invoke(result);
                SaveProgressionAsync();
                SyncBattleRewardAsync(result);
                OnInventoryChanged?.Invoke();
                return result;
            }

            foreach (DragonInstance dragon in battleDragons)
            {
                if (dragon?.Definition == null) continue;

                int beforeLevel = dragon.BondLevel;
                int xp = Mathf.RoundToInt(dragon.Definition.bondData?.battleBondXP ?? 10f);
                dragon.RecordBattle();
                int levelUps = Mathf.Max(0, dragon.BondLevel - beforeLevel);

                result.dragonsRewarded++;
                result.totalBondXp += xp;
                result.bondLevelUps += levelUps;
                result.dragonRewards.Add(new DragonBattleRewardEntry
                {
                    dragonId = dragon.Definition.dragonId,
                    displayName = dragon.Definition.displayName,
                    bondXpGranted = xp,
                    bondLevelBefore = beforeLevel,
                    bondLevelAfter = dragon.BondLevel,
                    totalBattles = dragon.TotalBattles
                });
            }

            string outcome = victory ? "Victory" : "Battle";
            string waveText = wavesCleared == 1 ? "1 wave" : $"{wavesCleared} waves";
            StageDefinition stage = StageCatalog.Get(GameManager.Instance?.CurrentStageId);
            float rewardMultiplier = Mathf.Max(1f, stage.rewardMultiplier);
            int baseEssenceGranted = Progression.GrantBattleEssence(wavesCleared, victory);
            int bonusEssence = Mathf.RoundToInt(baseEssenceGranted * (rewardMultiplier - 1f));
            if (bonusEssence > 0)
                Progression.AddEssence(bonusEssence);
            int essenceGranted = baseEssenceGranted + bonusEssence;
            int summonTicketsGranted = victory ? 1 : 0;
            if (summonTicketsGranted > 0)
                Progression.GrantSummonTickets(summonTicketsGranted);
            string ticketText = summonTicketsGranted > 0 ? $", +{summonTicketsGranted} summon ticket" : string.Empty;
            AwardBattleChest(result, victory);
            if (victory)
            {
                Progression.RecordDailyObjectiveProgress(DailyObjectiveType.WinBattle);
            }
            ApplyStageResult(result, victory);
            string chestText = BuildChestRewardSummary(result);
            string stageText = BuildStageRewardSummary(result);
            LastBattleRewardSummary = result.bondLevelUps > 0
                ? $"Progression: {outcome} ({waveText}) +{result.totalBondXp} bond XP, +{essenceGranted} essence{ticketText}{chestText}{stageText}, {result.bondLevelUps} bond level-up"
                : $"Progression: {outcome} ({waveText}) +{result.totalBondXp} bond XP, +{essenceGranted} essence{ticketText}{chestText}{stageText}";
            result.summary = LastBattleRewardSummary;
            LastBattleRewardResult = result;
            OnBattleRewardsGranted?.Invoke(LastBattleRewardSummary);
            OnBattleRewardResultGranted?.Invoke(result);
            SaveProgressionAsync();
            SyncBattleRewardAsync(result);
            OnInventoryChanged?.Invoke();
            return result;
        }

        private void AwardBattleChest(BattleRewardResult result, bool victory)
        {
            if (!victory || result == null)
                return;

            string rarity = RollBattleChestRarity(StageCatalog.Get(GameManager.Instance?.CurrentStageId));
            if (Progression.TryAwardBattleChest(rarity, out int slotIndex, out _))
            {
                result.chestAwarded = true;
                result.chestSlotIndex = slotIndex;
                result.chestRarity = rarity;
            }
        }

        private static string BuildChestRewardSummary(BattleRewardResult result)
        {
            if (result == null || !result.victory)
                return string.Empty;
            return result.chestAwarded
                ? $", +{result.chestRarity} chest"
                : ", chest slots full";
        }

        private void ApplyStageResult(BattleRewardResult result, bool victory)
        {
            if (result == null) return;

            StageDefinition stage = StageCatalog.Get(GameManager.Instance?.CurrentStageId);
            result.stageId = stage.stageId;
            result.stageTitle = stage.Title;
            result.bestStars = Progression.GetBestStageStars(stage.stageId);
            if (!victory)
            {
                result.objectiveSummary = "Stage failed";
                return;
            }

            BattleStatsTracker stats = BattleStatsTracker.Ensure();
            bool livesGoal = (GameManager.Instance?.Lives ?? 0) >= stage.tenLifeStarRequirement;
            bool leakGoal = stats.BattleEnemiesLeaked <= stage.maxLeaksForStar;
            bool dragonGoal = stats.BattleTowersPlaced <= stage.maxDragonsForStar;
            result.starsEarned = 1 + (livesGoal ? 1 : 0) + (leakGoal && dragonGoal ? 1 : 0);
            result.objectiveSummary =
                $"Stars {result.starsEarned}/3\n" +
                $"Win: complete\n" +
                $"Lives {GameManager.Instance?.Lives ?? 0}/{stage.tenLifeStarRequirement}: {(livesGoal ? "complete" : "missed")}\n" +
                $"Leaks {stats.BattleEnemiesLeaked}/{stage.maxLeaksForStar}: {(leakGoal ? "complete" : "missed")}\n" +
                $"Dragons {stats.BattleTowersPlaced}/{stage.maxDragonsForStar}: {(dragonGoal ? "complete" : "missed")}";

            result.firstClear = !Progression.IsStageCleared(stage.stageId);
            Progression.MarkStageCleared(stage.stageId);
            Progression.SetBestStageStars(stage.stageId, result.starsEarned);
            result.bestStars = Progression.GetBestStageStars(stage.stageId);

            int stageIndex = StageCatalog.GetIndex(stage.stageId) + 1;
            if (result.firstClear)
            {
                result.stageBonusGold += 150 * stageIndex;
                result.stageBonusGems += 10 * stageIndex;
                result.stageBonusEssence += 20 * stageIndex;
            }

            if (result.starsEarned >= 3)
            {
                result.stageBonusGems += 15 * stageIndex;
                result.stageBonusEssence += 25 * stageIndex;
                if (Random.Range(0, 100) < 35 + stageIndex * 5)
                    result.stageBonusSummonTickets += 1;
            }

            if (result.stageBonusGold > 0)
                Progression.AddGold(result.stageBonusGold);
            if (result.stageBonusGems > 0)
                Progression.AddGems(result.stageBonusGems);
            if (result.stageBonusEssence > 0)
                Progression.AddEssence(result.stageBonusEssence);
            if (result.stageBonusSummonTickets > 0)
                Progression.GrantSummonTickets(result.stageBonusSummonTickets);
        }

        private static string BuildStageRewardSummary(BattleRewardResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.stageId))
                return string.Empty;

            string bonus = result.stageBonusGold > 0 || result.stageBonusEssence > 0 || result.stageBonusGems > 0 || result.stageBonusSummonTickets > 0
                ? $", stage bonus +{result.stageBonusGold}g +{result.stageBonusEssence}e +{result.stageBonusGems} gems" +
                  (result.stageBonusSummonTickets > 0 ? $" +{result.stageBonusSummonTickets} ticket" : string.Empty)
                : string.Empty;
            return $", {result.starsEarned}/3 stars{bonus}";
        }

        private static string RollBattleChestRarity(StageDefinition stage)
        {
            int roll = Random.Range(0, 100);
            int stageIndex = StageCatalog.GetIndex(stage?.stageId);
            int commonCutoff = Mathf.Clamp(60 - stageIndex * 12, 20, 60);
            int rareCutoff = Mathf.Clamp(90 - stageIndex * 4, commonCutoff + 20, 94);
            int epicCutoff = Mathf.Clamp(99 - stageIndex, rareCutoff + 3, 99);
            if (roll < commonCutoff) return "Common";
            if (roll < rareCutoff) return "Rare";
            if (roll < epicCutoff) return "Epic";
            return "Legendary";
        }

        public bool TryUpgradeAccountBuff(AccountBuffType buffType, out string message)
        {
            bool upgraded = Progression.TryUpgrade(buffType, out message);
            if (upgraded)
                SaveProgressionAsync();
            return upgraded;
        }

        public bool TryLevelUpDragon(DragonInstance dragon, out string message)
        {
            if (dragon?.Definition == null)
            {
                message = "No dragon selected";
                return false;
            }

            const int maxLevel = 20;
            if (dragon.Level >= maxLevel)
            {
                message = $"{dragon.Definition.displayName} is max level";
                return false;
            }

            int cost = GetDragonLevelUpGoldCost(dragon);
            if (!Progression.TrySpendGold(cost, out message))
                return false;

            dragon.Level++;
            message = $"{dragon.Definition.displayName} reached Lv {dragon.Level}";
            SaveProgressionAsync();
            OnInventoryChanged?.Invoke();
            OnProgressionChanged?.Invoke();
            return true;
        }

        public bool TryTrainDragonBond(DragonInstance dragon, out string message)
        {
            if (dragon?.Definition == null)
            {
                message = "No dragon selected";
                return false;
            }

            int maxBond = dragon.Definition.bondData?.bondLevels?.Length ?? 7;
            if (dragon.BondLevel >= maxBond)
            {
                message = $"{dragon.Definition.displayName} bond is maxed";
                return false;
            }

            int cost = GetDragonBondTrainingEssenceCost(dragon);
            if (!Progression.TrySpendEssence(cost, out message))
                return false;

            int xp = Mathf.RoundToInt((dragon.Definition.bondData?.trainBondXP ?? 8f) * 10f);
            int before = dragon.BondLevel;
            dragon.AddBondXp(xp);
            message = dragon.BondLevel > before
                ? $"{dragon.Definition.displayName} gained {xp} bond XP and reached Bond {dragon.BondLevel}"
                : $"{dragon.Definition.displayName} gained {xp} bond XP";
            SaveProgressionAsync();
            OnInventoryChanged?.Invoke();
            OnProgressionChanged?.Invoke();
            return true;
        }

        public bool TryEvolveDragon(DragonInstance dragon, out string message)
        {
            if (dragon?.Definition == null)
            {
                message = "No dragon selected";
                return false;
            }

            int currentStage = (int)dragon.EvolutionStage;
            int maxStage = System.Enum.GetValues(typeof(DragonEvolutionStage)).Length - 1;
            if (currentStage >= maxStage)
            {
                message = $"{dragon.Definition.displayName} is fully evolved";
                return false;
            }

            if (dragon.BondLevel < 5)
            {
                message = "Bond 5 required to evolve";
                return false;
            }

            int cost = GetDragonEvolutionEssenceCost(dragon);
            if (!Progression.TrySpendEssence(cost, out message))
                return false;

            dragon.EvolutionStage = (DragonEvolutionStage)(currentStage + 1);
            message = $"{dragon.Definition.displayName} evolved to {dragon.EvolutionStage}";
            SaveProgressionAsync();
            OnInventoryChanged?.Invoke();
            OnProgressionChanged?.Invoke();
            return true;
        }

        public static int GetDragonLevelUpGoldCost(DragonInstance dragon)
        {
            int level = Mathf.Max(1, dragon?.Level ?? 1);
            return 80 + level * 40;
        }

        public static int GetDragonBondTrainingEssenceCost(DragonInstance dragon)
        {
            int bond = Mathf.Max(1, dragon?.BondLevel ?? 1);
            return 20 + bond * 15;
        }

        public static int GetDragonEvolutionEssenceCost(DragonInstance dragon)
        {
            int stage = Mathf.Max(0, (int)(dragon?.EvolutionStage ?? DragonEvolutionStage.Hatchling));
            return 100 + stage * 100;
        }

        public static string PresetLabel(LoadoutPresetType preset) => preset switch
        {
            LoadoutPresetType.Balanced => "Balanced",
            LoadoutPresetType.Boss => "Boss",
            LoadoutPresetType.FastEnemies => "Fast Enemies",
            LoadoutPresetType.ShieldBreak => "Shield Break",
            _ => "Preset"
        };

        private static DragonRoleTag[] GetPresetPriorities(LoadoutPresetType preset) => preset switch
        {
            LoadoutPresetType.Balanced => new[] { DragonRoleTag.Damage, DragonRoleTag.Slow, DragonRoleTag.Aoe, DragonRoleTag.Support, DragonRoleTag.AntiShield, DragonRoleTag.AntiFlying },
            LoadoutPresetType.Boss => new[] { DragonRoleTag.Damage, DragonRoleTag.Damage, DragonRoleTag.Support, DragonRoleTag.AntiShield, DragonRoleTag.Aoe, DragonRoleTag.AntiFlying },
            LoadoutPresetType.FastEnemies => new[] { DragonRoleTag.Slow, DragonRoleTag.Aoe, DragonRoleTag.AntiFlying, DragonRoleTag.Damage, DragonRoleTag.Support, DragonRoleTag.AntiShield },
            LoadoutPresetType.ShieldBreak => new[] { DragonRoleTag.AntiShield, DragonRoleTag.Damage, DragonRoleTag.Aoe, DragonRoleTag.Support, DragonRoleTag.Slow, DragonRoleTag.AntiFlying },
            _ => new[] { DragonRoleTag.Damage }
        };

        private void AddBestDragonForRole(List<DragonInstance> selected, DragonRoleTag role)
        {
            DragonInstance best = null;
            float bestScore = float.MinValue;
            foreach (DragonInstance dragon in OwnedDragons)
            {
                if (dragon?.Definition == null || selected.Contains(dragon)) continue;
                if (!DragonRoleUtility.HasRole(dragon, role)) continue;

                float score = dragon.Attack + dragon.AttackSpeed * 50f + dragon.Level * 20f + dragon.BondLevel * 15f + (int)dragon.Definition.rarity * 35f;
                if (score > bestScore)
                {
                    best = dragon;
                    bestScore = score;
                }
            }

            if (best != null)
                selected.Add(best);
        }

        public void AddStoreGems(int amount)
        {
            Progression.AddGems(amount);
            SaveProgressionAsync();
            OnProgressionChanged?.Invoke();
        }

        public async Task<StorePurchaseResult> PurchaseGemPackAsync(string productId)
        {
            if (!GemStoreCatalog.TryGetPack(productId, out GemPackDefinition pack))
                return StorePurchaseResult.Fail("Unknown product", productId);

            if (_purchaseService == null)
                _purchaseService = CreatePurchaseService(new LocalIapReceiptValidator());

            SetSyncStatus("Validating purchase...");
            StorePurchaseResult result = await _purchaseService.PurchaseGemPackAsync(pack);
            if (result == null)
            {
                SetSyncStatus("Purchase failed");
                return StorePurchaseResult.Fail("Purchase validation returned no result", productId);
            }

            if (!result.success || !result.validated)
            {
                SetSyncStatus("Purchase failed");
                return result;
            }

            Progression.AddGems(result.gemsGranted);
            await SaveProgressionAsync();
            OnProgressionChanged?.Invoke();
            SetSyncStatus("Purchase saved");
            return result;
        }

        public async Task<bool> LoginWithDeviceAndUseApiAsync(string baseUrl, string deviceId, string displayName)
        {
            var authService = new ApiAuthService(baseUrl);
            SetSyncStatus("Signing in...");
            _authSession = await authService.LoginWithDeviceAsync(deviceId, displayName);
            if (_authSession == null || !_authSession.IsAuthenticated)
            {
                SetSyncStatus("Sign-in failed");
                return false;
            }

            _persistenceService = new ApiProgressionPersistenceService(baseUrl, _authSession);
            _purchaseService = CreatePurchaseService(new ApiIapReceiptValidator(baseUrl, _authSession));
            SetSyncStatus(_persistenceService.ModeLabel);
            await SaveProgressionAsync();
            return true;
        }

        public void UseLocalPersistence()
        {
            _persistenceService = new LocalProgressionPersistenceService(SavePath);
            _purchaseService = CreatePurchaseService(new LocalIapReceiptValidator());
            _authSession = null;
            SetSyncStatus(_persistenceService.ModeLabel);
        }

        private static IStorePurchaseService CreatePurchaseService(IIapReceiptValidator validator)
        {
#if UNITY_EDITOR
            return new EditorMockIapPurchaseService(validator);
#else
            return new UnityIapReceiptCaptureService(validator);
#endif
        }

        public bool TryStartChestUnlock(int slotIndex, out string message)
        {
            bool changed = Progression.TryStartChestUnlock(slotIndex, out message);
            if (changed)
            {
                SaveProgressionAsync();
                OnProgressionChanged?.Invoke();
            }
            return changed;
        }

        public bool TrySelectStage(string stageId, out string message)
        {
            bool selected = Progression.TrySelectStage(stageId, out message);
            if (selected)
            {
                SaveProgressionAsync();
                OnProgressionChanged?.Invoke();
            }
            return selected;
        }

        public bool TryClaimDailyObjective(string objectiveId, out string message)
        {
            bool claimed = Progression.TryClaimDailyObjective(objectiveId, out message);
            if (claimed)
            {
                SaveProgressionAsync();
                OnProgressionChanged?.Invoke();
            }
            return claimed;
        }

        public bool TryOpenChest(int slotIndex, out string message)
        {
            bool changed = Progression.TryOpenChest(slotIndex, out message);
            if (changed)
            {
                Progression.RecordDailyObjectiveProgress(DailyObjectiveType.OpenChest);
                SaveProgressionAsync();
                OnProgressionChanged?.Invoke();
            }
            return changed;
        }

        public bool TryOpenChest(int slotIndex, out ChestRewardResult reward, out string message)
        {
            bool changed = Progression.TryOpenChest(slotIndex, out reward, out message);
            if (changed)
            {
                Progression.RecordDailyObjectiveProgress(DailyObjectiveType.OpenChest);
                SaveProgressionAsync();
                OnProgressionChanged?.Invoke();
            }
            return changed;
        }

        public bool TrySpeedUpChestUnlock(int slotIndex, out string message)
        {
            bool changed = Progression.TrySpeedUpChestUnlock(slotIndex, out message);
            if (changed)
            {
                SaveProgressionAsync();
                OnProgressionChanged?.Invoke();
            }
            return changed;
        }

        public bool TryClaimEventReward(PrototypeEventDefinition eventDefinition, out string message)
        {
            bool changed = Progression.TryClaimEventReward(eventDefinition, out message);
            if (changed)
            {
                SaveProgressionAsync();
                OnProgressionChanged?.Invoke();
            }
            return changed;
        }

        public void ResetLocalProgression()
        {
            _persistenceService?.ResetProgression();

            OwnedDragons.Clear();
            LastBattleRewardSummary = string.Empty;
            LastSummonSummary = string.Empty;
            LastBattleRewardResult = null;
            Progression.Load(null);
            SaveProgressionAsync();
            OnInventoryChanged?.Invoke();
            OnProgressionChanged?.Invoke();
            OnLoadoutChanged?.Invoke();
        }

        public async void SaveLocalProgression()
        {
            await SaveProgressionAsync();
        }

        public async Task<bool> SaveProgressionAsync()
        {
            SetSyncStatus("Saving...");
            ProgressionPersistenceResult result = await _persistenceService.SaveProgressionAsync(BuildSaveData());
            SetSyncStatus(result.success ? "Saved" : "Sync failed");
            if (!result.success)
                Debug.LogWarning($"[PlayerInventory] {result.message}");
            return result.success;
        }

        public PlayerProgressionApiDto BuildApiDto()
        {
            PlayerProgressionSaveData saveData = BuildSaveData();
            var dto = new PlayerProgressionApiDto
            {
                version = saveData.version,
                essence = saveData.essence,
                gold = saveData.gold,
                gems = saveData.gems,
                summon_tickets = saveData.summonTickets,
                damage_buff_level = saveData.damageBuffLevel,
                attack_speed_buff_level = saveData.attackSpeedBuffLevel,
                starting_mana_buff_level = saveData.startingManaBuffLevel,
                current_stage_id = saveData.currentStageId,
                highest_unlocked_stage_index = saveData.highestUnlockedStageIndex,
                cleared_stage_ids = new List<string>(saveData.clearedStageIds ?? new List<string>()),
                stage_stars = ToApiDto(saveData.stageStars),
                equipped_dragon_ids = new List<string>(saveData.equippedDragonIds ?? new List<string>()),
                chest_slots = ToApiDto(saveData.chestSlots),
                event_claims = ToApiDto(saveData.eventClaims),
                daily_objectives = ToApiDto(saveData.dailyObjectives),
                last_battle_reward = ToApiDto(saveData.lastBattleReward)
            };

            foreach (DragonProgressionSaveData dragon in saveData.dragons)
            {
                dto.dragons.Add(new DragonProgressionApiDto
                {
                    dragon_id = dragon.dragonId,
                    level = dragon.level,
                    bond_level = dragon.bondLevel,
                    bond_xp = dragon.bondXp,
                    total_battles = dragon.totalBattles,
                    evolution_stage = dragon.evolutionStage,
                    skill_level = dragon.skillLevel
                });
            }

            return dto;
        }

        private async Task<bool> LoadProgressionAsync()
        {
            ProgressionPersistenceResult result = await _persistenceService.LoadProgressionAsync();
            if (!result.success || result.saveData == null)
            {
                SetSyncStatus(_persistenceService.ModeLabel);
                return false;
            }

            ApplySaveData(result.saveData);
            SetSyncStatus("Loaded");
            return true;
        }

        private async void SyncBattleRewardAsync(BattleRewardResult reward)
        {
            ProgressionPersistenceResult result = await _persistenceService.SyncBattleRewardAsync(BuildApiDto(), reward);
            SetSyncStatus(result.success ? "Saved" : "Sync failed");
        }

        private void ApplySaveData(PlayerProgressionSaveData saveData)
        {
            OwnedDragons.Clear();
            if (saveData.dragons != null)
            {
                foreach (DragonProgressionSaveData dragonSave in saveData.dragons)
                {
                    DragonDefinition definition = FindStarterDragon(dragonSave.dragonId);
                    if (definition == null) continue;

                    OwnedDragons.Add(new DragonInstance
                    {
                        Definition = definition,
                        Level = Mathf.Max(1, dragonSave.level),
                        BondLevel = Mathf.Max(1, dragonSave.bondLevel),
                        BondXp = Mathf.Max(0f, dragonSave.bondXp),
                        TotalBattles = System.Math.Max(0L, dragonSave.totalBattles),
                        EvolutionStage = (DragonEvolutionStage)Mathf.Clamp(dragonSave.evolutionStage, 0, System.Enum.GetValues(typeof(DragonEvolutionStage)).Length - 1),
                        SkillLevel = Mathf.Clamp(dragonSave.skillLevel, 1, 10)
                    });
                }
            }

            EquippedDragonIds.Clear();
            if (saveData.equippedDragonIds != null)
                EquippedDragonIds.AddRange(saveData.equippedDragonIds);
            EnsureValidLoadout();

            Progression.Load(saveData);
            if (OwnedDragons.Count == 0 && Progression.SummonTickets == 0)
                Progression.GrantSummonTickets(PlayerProgression.StartingSummonTickets);
            LastBattleRewardResult = saveData.lastBattleReward;
            LastBattleRewardSummary = saveData.lastBattleReward?.summary ?? string.Empty;
            OnInventoryChanged?.Invoke();
            OnProgressionChanged?.Invoke();
            OnLoadoutChanged?.Invoke();
        }

        private PlayerProgressionSaveData BuildSaveData()
        {
            var saveData = new PlayerProgressionSaveData
            {
                lastBattleReward = LastBattleRewardResult
            };
            Progression.WriteTo(saveData);
            EnsureValidLoadout();
            saveData.equippedDragonIds = new List<string>(EquippedDragonIds);

            foreach (DragonInstance dragon in OwnedDragons)
            {
                if (dragon?.Definition == null) continue;

                saveData.dragons.Add(new DragonProgressionSaveData
                {
                    dragonId = dragon.Definition.dragonId,
                    level = dragon.Level,
                    bondLevel = dragon.BondLevel,
                    bondXp = dragon.BondXp,
                    totalBattles = dragon.TotalBattles,
                    evolutionStage = (int)dragon.EvolutionStage,
                    skillLevel = dragon.SkillLevel
                });
            }

            return saveData;
        }

        private static BattleRewardApiDto ToApiDto(BattleRewardResult result)
        {
            if (result == null) return null;

            var dto = new BattleRewardApiDto
            {
                victory = result.victory,
                waves_cleared = result.wavesCleared,
                dragons_rewarded = result.dragonsRewarded,
                total_bond_xp = result.totalBondXp,
                bond_level_ups = result.bondLevelUps,
                stage_id = result.stageId,
                stage_title = result.stageTitle,
                stars_earned = result.starsEarned,
                best_stars = result.bestStars,
                first_clear = result.firstClear,
                objective_summary = result.objectiveSummary,
                stage_bonus_gold = result.stageBonusGold,
                stage_bonus_essence = result.stageBonusEssence,
                stage_bonus_gems = result.stageBonusGems,
                stage_bonus_summon_tickets = result.stageBonusSummonTickets,
                chest_awarded = result.chestAwarded,
                chest_slot_index = result.chestSlotIndex,
                chest_rarity = result.chestRarity,
                summary = result.summary
            };

            if (result.dragonRewards == null) return dto;
            foreach (DragonBattleRewardEntry reward in result.dragonRewards)
            {
                dto.dragon_rewards.Add(new DragonBattleRewardApiDto
                {
                    dragon_id = reward.dragonId,
                    display_name = reward.displayName,
                    bond_xp_granted = reward.bondXpGranted,
                    bond_level_before = reward.bondLevelBefore,
                    bond_level_after = reward.bondLevelAfter,
                    total_battles = reward.totalBattles
                });
            }

            return dto;
        }

        private static List<ChestSlotApiDto> ToApiDto(List<ChestSlotSaveData> chestSlots)
        {
            var dto = new List<ChestSlotApiDto>();
            if (chestSlots == null) return dto;

            foreach (ChestSlotSaveData slot in chestSlots)
            {
                if (slot == null) continue;
                dto.Add(new ChestSlotApiDto
                {
                    rarity = slot.rarity,
                    unlock_complete_utc_ticks = slot.unlockCompleteUtcTicks,
                    claimed = slot.claimed
                });
            }

            return dto;
        }

        private static List<StageStarApiDto> ToApiDto(List<StageStarSaveData> stageStars)
        {
            var dto = new List<StageStarApiDto>();
            if (stageStars == null) return dto;

            foreach (StageStarSaveData save in stageStars)
            {
                if (save == null) continue;
                dto.Add(new StageStarApiDto
                {
                    stage_id = save.stageId,
                    best_stars = save.bestStars
                });
            }

            return dto;
        }

        private static List<EventClaimApiDto> ToApiDto(List<EventClaimSaveData> eventClaims)
        {
            var dto = new List<EventClaimApiDto>();
            if (eventClaims == null) return dto;

            foreach (EventClaimSaveData claim in eventClaims)
            {
                if (claim == null) continue;
                dto.Add(new EventClaimApiDto
                {
                    event_id = claim.eventId,
                    last_claim_utc_ticks = claim.lastClaimUtcTicks,
                    claim_count = claim.claimCount
                });
            }

            return dto;
        }

        private static List<DailyObjectiveApiDto> ToApiDto(List<DailyObjectiveSaveData> objectives)
        {
            var dto = new List<DailyObjectiveApiDto>();
            if (objectives == null) return dto;

            foreach (DailyObjectiveSaveData objective in objectives)
            {
                if (objective == null) continue;
                dto.Add(new DailyObjectiveApiDto
                {
                    objective_id = objective.objectiveId,
                    date_utc_ticks = objective.dateUtcTicks,
                    progress = objective.progress,
                    claimed = objective.claimed
                });
            }

            return dto;
        }

        private void SeedStarterDragons()
        {
            if (_starterDragons == null) return;
            foreach (DragonDefinition def in _starterDragons)
            {
                if (def == null) continue;
                var dragon = new DragonInstance { Definition = def };
                OwnedDragons.Add(dragon);
                EquipDefaultIfSlotAvailable(dragon);
            }
            OnInventoryChanged?.Invoke();
            OnLoadoutChanged?.Invoke();
        }

        private void AbsorbStarterDragons(DragonDefinition[] starterDragons)
        {
            if ((starterDragons == null || starterDragons.Length == 0) && HasStarterDragons())
                return;

            if (!HasStarterDragons() && starterDragons != null)
                _starterDragons = starterDragons;

            if (OwnedDragons.Count == 0)
                SeedStarterDragons();
        }

        private bool HasStarterDragons()
        {
            if (_starterDragons == null) return false;
            foreach (DragonDefinition def in _starterDragons)
            {
                if (def != null)
                    return true;
            }
            return false;
        }

        private void EnsureValidLoadout()
        {
            for (int i = EquippedDragonIds.Count - 1; i >= 0; i--)
            {
                if (FindOwnedDragon(EquippedDragonIds[i]) == null || EquippedDragonIds.IndexOf(EquippedDragonIds[i]) != i)
                    EquippedDragonIds.RemoveAt(i);
            }

            if (EquippedDragonIds.Count > 0)
                return;

            foreach (DragonInstance dragon in OwnedDragons)
            {
                if (EquippedDragonIds.Count >= MaxEquippedDragons)
                    break;
                EquipDefaultIfSlotAvailable(dragon);
            }
        }

        private void EquipDefaultIfSlotAvailable(DragonInstance dragon)
        {
            string dragonId = dragon?.Definition?.dragonId;
            if (string.IsNullOrWhiteSpace(dragonId)) return;
            if (EquippedDragonIds.Count >= MaxEquippedDragons) return;
            if (EquippedDragonIds.Contains(dragonId)) return;
            EquippedDragonIds.Add(dragonId);
        }

        private DragonInstance FindOwnedDragon(string dragonId)
        {
            if (string.IsNullOrWhiteSpace(dragonId)) return null;
            foreach (DragonInstance dragon in OwnedDragons)
            {
                if (dragon?.Definition != null && dragon.Definition.dragonId == dragonId)
                    return dragon;
            }
            return null;
        }

        private DragonDefinition PickSummonDragon()
        {
            EnsureStarterDragonsAvailable();
            if (_starterDragons == null) return null;

            var unowned = new List<DragonDefinition>();
            foreach (DragonDefinition def in _starterDragons)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.dragonId)) continue;
                if (FindOwnedDragon(def.dragonId) == null)
                    unowned.Add(def);
            }

            return unowned.Count > 0 ? unowned[Random.Range(0, unowned.Count)] : null;
        }

        private DragonDefinition FindStarterDragon(string dragonId)
        {
            EnsureStarterDragonsAvailable();
            if (string.IsNullOrWhiteSpace(dragonId) || _starterDragons == null) return null;
            foreach (DragonDefinition def in _starterDragons)
            {
                if (def != null && def.dragonId == dragonId)
                    return def;
            }
            return null;
        }

        private void EnsureStarterDragonsAvailable()
        {
            if (HasStarterDragons()) return;

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:DragonDefinition", new[] { "Assets/ScriptableObjects/Dragons" });
            var dragons = new List<DragonDefinition>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                DragonDefinition def = UnityEditor.AssetDatabase.LoadAssetAtPath<DragonDefinition>(path);
                if (def != null)
                    dragons.Add(def);
            }

            if (dragons.Count > 0)
                _starterDragons = dragons.ToArray();
#endif
        }

        private void HandleProgressionChanged()
        {
            OnProgressionChanged?.Invoke();
        }

        private void SetSyncStatus(string status)
        {
            SyncStatus = string.IsNullOrWhiteSpace(status) ? _persistenceService?.ModeLabel ?? "Local" : status;
            OnSyncStatusChanged?.Invoke(SyncStatus);
        }
    }
}
