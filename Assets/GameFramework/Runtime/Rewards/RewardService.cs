using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Rewards
{
    /// <summary>
    /// Default <see cref="IRewardService"/>. Idempotency for a <see cref="RewardClaimPolicy.Once"/>
    /// reward is a persisted <see cref="HashSet{T}"/> of claimed ids — the exact same "check the
    /// flag, then set it" shape <c>UnlockService</c> uses for its unlocked-set, and the same
    /// explicit-Save-Load-with-dirty-flag persistence policy every Phase 6 service follows.
    /// Transaction safety is validate-then-mutate: <see cref="IReward.CanGrant"/> is checked (which
    /// recurses through an entire <see cref="RewardBundle"/>) before <see cref="IReward.Grant"/> is
    /// ever called, so a reward already known to be invalid never partially mutates player state.
    /// This is not a rollback engine - if a child's <c>Grant</c> still fails after its own
    /// <c>CanGrant</c> passed (which should not happen in this single-threaded, main-thread-only
    /// framework - see the project's threading rules), whatever already granted before it stays
    /// granted, and the failure is logged loudly rather than hidden.
    /// </summary>
    public sealed class RewardService : IRewardService
    {
        private const string LogCategory = "Rewards";
        private const string SaveKey = "GameFramework.Rewards";
        private const int SaveVersion = 1;

        private sealed class Entry
        {
            public RewardDefinition Definition;
            public IReward Content;
        }

        private readonly Dictionary<RewardId, Entry> _entries = new Dictionary<RewardId, Entry>();
        private readonly HashSet<RewardId> _claimed = new HashSet<RewardId>();

        private IPersistenceService _persistence;
        private IEventService _events;
        private ILoggingService _log;
        private bool _isDirty;

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);
            Load();
        }

        public void Shutdown()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        public void RegisterReward(RewardDefinition definition, IReward content)
        {
            Guard.NotNull(definition, nameof(definition));
            Guard.NotNull(content, nameof(content));

            RewardId id = definition.Id;
            if (!id.IsValid)
            {
                throw new System.ArgumentException("RewardDefinition has no Id assigned.", nameof(definition));
            }

            if (_entries.ContainsKey(id))
            {
                throw new System.InvalidOperationException($"Duplicate reward id '{id}'.");
            }

            _entries.Add(id, new Entry { Definition = definition, Content = content });
        }

        public bool IsRegistered(RewardId id) => _entries.ContainsKey(id);

        public bool HasClaimed(RewardId id) => _claimed.Contains(id);

        public RewardClaimResult TryClaim(RewardId id, string reason = null)
        {
            if (!_entries.TryGetValue(id, out Entry entry))
            {
                return RewardClaimResult.InvalidId;
            }

            if (entry.Definition.ClaimPolicy == RewardClaimPolicy.Once && _claimed.Contains(id))
            {
                return RewardClaimResult.AlreadyClaimed;
            }

            if (!entry.Content.CanGrant())
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"TryClaim('{id}') rejected: reward content cannot currently be granted.");
                return RewardClaimResult.GrantFailed;
            }

            RewardGrantResult grantResult = entry.Content.Grant();
            if (!grantResult.Success)
            {
                _log?.Log(LogLevel.Error, LogCategory,
                    $"Reward '{id}' failed to grant after CanGrant() passed: {grantResult.FailureDetail}.");
                return RewardClaimResult.GrantFailed;
            }

            _events.Publish(new RewardGrantedEvent(id, reason));

            if (entry.Definition.ClaimPolicy == RewardClaimPolicy.Once)
            {
                _claimed.Add(id);
                _isDirty = true;
                _events.Publish(new RewardClaimedEvent(id, reason));
            }

            return RewardClaimResult.Success;
        }

        public void Save()
        {
            var data = new RewardSaveData();
            foreach (RewardId id in _claimed)
            {
                data.ClaimedRewardIds.Add(id.Value);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            // Deliberately does not filter against _entries: like UnlockService.RegisterUnlock,
            // RegisterReward necessarily happens after Initialize/Load, so _entries is always empty
            // at this point - see UnlockService's remarks for the full explanation.
            RewardSaveData data = _persistence.Load(SaveKey, SaveVersion, new RewardSaveData());
            _claimed.Clear();

            foreach (string idValue in data.ClaimedRewardIds)
            {
                _claimed.Add(new RewardId(idValue));
            }

            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            if (_claimed.Count == 0)
            {
                return;
            }

            _claimed.Clear();
            _isDirty = true;
        }
    }
}
