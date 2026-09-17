using System.Collections;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using GameFramework.Rewards;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using GameFramework.Unlocks;
using UnityEngine;

namespace GameFramework.Samples.Phase6Demo
{
    /// <summary>
    /// Drives the Phase 6 demonstration scene: exercises Economy, Inventory, Experience, Unlocks,
    /// and Rewards together, using the five content assets under <c>Content/</c> (the framework's
    /// own <c>ProgressionBootstrapper</c> only registers the services with that static content -
    /// see this class for the game-specific step of registering an actual unlock requirement and
    /// reward bundle, done here after Ready for the reason documented on
    /// <see cref="ProgressionBootstrapper"/>). Not part of the reusable framework - sample/demo
    /// content only, kept in its own assembly and scene, separate from any production game content.
    /// </summary>
    public sealed class Phase6DemoController : MonoBehaviour
    {
        private const string LogCategory = "Phase6Demo";
        private static readonly CurrencyId Coins = new CurrencyId("Coins");
        private static readonly ItemId Potion = new ItemId("HealthPotion");
        private static readonly UnlockId VeteranCar = new UnlockId("VeteranCar");
        private static readonly RewardId FirstQuest = new RewardId("FirstQuest");

        [Tooltip("Assigned from Content/VeteranCarUnlock.asset - the same assets ProgressionBootstrapper's own fields reference.")]
        [SerializeField] private UnlockDefinition _veteranCarUnlockDefinition;

        [Tooltip("Assigned from Content/FirstQuestReward.asset.")]
        [SerializeField] private RewardDefinition _firstQuestRewardDefinition;

        private IEconomyService _economy;
        private IInventoryService _inventory;
        private IExperienceService _experience;
        private IUnlockService _unlocks;
        private IRewardService _rewards;
        private bool _ready;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _economy = services.Get<IEconomyService>();
            _inventory = services.Get<IInventoryService>();
            _experience = services.Get<IExperienceService>();
            _unlocks = services.Get<IUnlockService>();
            _rewards = services.Get<IRewardService>();

            RegisterDemoContent();

            _ready = true;
            Debug.Log("[Phase6Demo] Ready. Keys: 1 = claim quest reward, 2 = claim again (idempotency), " +
                "3 = try unlock car (before level 3), G = grant XP to reach level 3, U = try unlock car again, " +
                "R = full status report.");
            LogStatus();
        }

        private void RegisterDemoContent()
        {
            // The car requires level 3 - this is the game-specific content step ProgressionBootstrapper
            // deliberately does not do itself (see its remarks).
            _unlocks.RegisterUnlock(_veteranCarUnlockDefinition, new LevelRequirement(_experience, minLevel: 3));

            _rewards.RegisterReward(_firstQuestRewardDefinition, new RewardBundle(
                new CurrencyReward(_economy, Coins, 100),
                new ItemReward(_inventory, Potion, 1),
                new ExperienceReward(_experience, 50)));
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                RewardClaimResult result = _rewards.TryClaim(FirstQuest, "Demo");
                Debug.Log($"[Phase6Demo] TryClaim(FirstQuest) -> {result}");
                LogStatus();
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                RewardClaimResult result = _rewards.TryClaim(FirstQuest, "Demo");
                Debug.Log($"[Phase6Demo] TryClaim(FirstQuest) again -> {result} (expected AlreadyClaimed).");
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                UnlockResult result = _unlocks.TryUnlock(VeteranCar, "Demo");
                string blockingReason = _unlocks.GetBlockingReason(VeteranCar);
                Debug.Log($"[Phase6Demo] TryUnlock(VeteranCar) -> {result}" +
                    (blockingReason != null ? $" (blocked: {blockingReason})" : string.Empty));
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                AddExperienceResult result = _experience.AddExperience(500, "Demo");
                Debug.Log($"[Phase6Demo] AddExperience(500) -> {result}. Now level {_experience.CurrentLevel}.");
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                UnlockResult result = _unlocks.TryUnlock(VeteranCar, "Demo");
                Debug.Log($"[Phase6Demo] TryUnlock(VeteranCar) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                LogStatus();
            }
        }

        private void LogStatus()
        {
            Debug.Log($"[Phase6Demo] Status: Coins={_economy.GetBalance(Coins)}, " +
                $"Potions={_inventory.GetQuantity(Potion)}, Level={_experience.CurrentLevel} " +
                $"(XP {_experience.CurrentExperience}/{_experience.CurrentExperience + _experience.ExperienceToNextLevel}), " +
                $"VeteranCarUnlocked={_unlocks.IsUnlocked(VeteranCar)}, QuestClaimed={_rewards.HasClaimed(FirstQuest)}.");
        }

    }
}
