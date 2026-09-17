using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using GameFramework.Unlocks;
using UnityEngine;

namespace GameFramework.Rewards
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 6's five
    /// application-level services (<see cref="IEconomyService"/>, <see cref="IInventoryService"/>,
    /// <see cref="IExperienceService"/>, <see cref="IUnlockService"/>, <see cref="IRewardService"/>)
    /// - the same registration-extension mechanism every other phase's bootstrapper subclass uses
    /// (compare <c>PlayerSystemsBootstrapper</c>, <c>GameplayBootstrapper</c>, <c>PerformanceBootstrapper</c>).
    ///
    /// Only registers the five services with their static content (currencies/items/the
    /// progression curve) - it deliberately does <b>not</b> register any
    /// <see cref="UnlockDefinition"/>/<see cref="RewardDefinition"/> requirements or contents,
    /// since which unlocks/rewards exist and what they require is game-specific content, not
    /// framework composition. A game (or this framework's own Phase 6 sample) does that separately,
    /// after <see cref="GameBootstrapper.State"/> reaches <see cref="BootstrapState.Ready"/> - the
    /// same "wait for Ready, then wire content" pattern every phase's demo scene already uses,
    /// necessary here because requirement/reward content needs to call
    /// <see cref="IServiceRegistry.Get{TService}"/> on these five services, which only succeeds
    /// once each is actually initialized.
    ///
    /// Sibling, not a base or subclass, of the other bootstrappers: nothing in
    /// <see cref="GameFramework.Progression"/>/<see cref="GameFramework.Unlocks"/>/
    /// <see cref="GameFramework.Rewards"/> references Input/UI/Audio/Feedback/Gameplay/Performance
    /// types. A game wanting Progression alongside Player Systems/Gameplay/Performance combines
    /// them in its own small subclass, exactly as the framework already documents for combining any
    /// two of those.
    /// </summary>
    public class ProgressionBootstrapper : GameBootstrapper
    {
        [Header("Progression Content")]
        [SerializeField] private CurrencyDefinition[] _currencies = System.Array.Empty<CurrencyDefinition>();
        [SerializeField] private ItemDefinition[] _items = System.Array.Empty<ItemDefinition>();
        [SerializeField] private ProgressionCurveDefinition _progressionCurve;

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IEconomyService>(new EconomyService(_currencies));
            registry.Register<IInventoryService>(new InventoryService(_items));
            registry.Register<IExperienceService>(new ExperienceService(RequireCurve()));
            registry.Register<IUnlockService>(new UnlockService());
            registry.Register<IRewardService>(new RewardService());
        }

        private IProgressionCurve RequireCurve()
        {
            if (_progressionCurve == null)
            {
                Debug.LogError($"[ProgressionBootstrapper] '{name}' has no Progression Curve assigned; " +
                    "IExperienceService will use a curve that always reports level 1 as the maximum.", this);
                return new EmptyProgressionCurve();
            }

            return _progressionCurve;
        }

        private sealed class EmptyProgressionCurve : IProgressionCurve
        {
            public int GetRequiredExperience(int level) => 0;
        }
    }
}
