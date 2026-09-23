using System;
using System.Collections.Generic;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Monetization.Entitlements
{
    /// <summary>
    /// Default <see cref="IEntitlementService"/>. Persists directly through
    /// <see cref="IPersistenceService"/> (the same pattern <c>Rewards.RewardService</c>/
    /// <c>Unlocks.UnlockService</c> use) rather than through a <c>PlayerData</c> profile - see
    /// CLAUDE.md's Phase 15 brief on Player Data integration: a game that wants entitlement state
    /// scoped per profile wraps this service's snapshot in its own <c>PlayerDataSection</c> adapter,
    /// the same non-retrofit precedent Phase 13 already established for Settings/Economy/Inventory.
    /// </summary>
    public sealed class EntitlementService : IEntitlementService
    {
        private const string LogCategory = "Monetization.Entitlements";
        private const string SaveKey = "GameFramework.Monetization.Entitlements";
        private const int SaveVersion = 1;

        private readonly Dictionary<string, EntitlementState> _entitlements = new Dictionary<string, EntitlementState>(StringComparer.Ordinal);

        // Runtime-only, never persisted: ids a provider-backed flow (purchase/restore/sync) reported in
        // this process. Deliberately not saved, so editing the save file can never manufacture it.
        private readonly HashSet<string> _verifiedThisSession = new HashSet<string>(StringComparer.Ordinal);

        private IPersistenceService _persistence;
        private IEventService _events;
        private ILoggingService _log;
        private bool _isDirty;

        public IReadOnlyList<EntitlementId> OwnedEntitlements => BuildOwnedList();

        public event Action<EntitlementId, bool> EntitlementChanged;

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

        public bool HasEntitlement(EntitlementId id)
        {
            if (!id.IsValid || !_entitlements.TryGetValue(id.Value, out EntitlementState state))
            {
                return false;
            }

            return state.IsCurrentlyActive(DateTime.UtcNow);
        }

        public bool IsVerifiedThisSession(EntitlementId id) => id.IsValid && _verifiedThisSession.Contains(id.Value);

        public EntitlementState GetEntitlement(EntitlementId id)
        {
            if (id.IsValid && _entitlements.TryGetValue(id.Value, out EntitlementState state))
            {
                return state;
            }

            return EntitlementState.NotOwned(id);
        }

        public void GrantEntitlement(EntitlementId id, EntitlementSource source, DateTime? expirationUtc = null, bool isAutoRenewing = false)
        {
            if (!id.IsValid)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "GrantEntitlement called with an invalid EntitlementId.");
                return;
            }

            _entitlements[id.Value] = new EntitlementState(id, true, expirationUtc, isAutoRenewing, source);
            if (source != EntitlementSource.Granted)
            {
                _verifiedThisSession.Add(id.Value);
            }

            _isDirty = true;
            RaiseChanged(id, true);
        }

        public void RevokeEntitlement(EntitlementId id)
        {
            if (!id.IsValid || !_entitlements.TryGetValue(id.Value, out EntitlementState state) || !state.IsOwned)
            {
                return;
            }

            _entitlements[id.Value] = new EntitlementState(id, false, null, false, state.Source);
            _isDirty = true;
            RaiseChanged(id, false);
        }

        public void SyncFromProvider(IReadOnlyList<EntitlementState> ownedEntitlements)
        {
            if (ownedEntitlements == null)
            {
                return;
            }

            for (int i = 0; i < ownedEntitlements.Count; i++)
            {
                EntitlementState reported = ownedEntitlements[i];
                if (!reported.Id.IsValid)
                {
                    continue;
                }

                _entitlements[reported.Id.Value] = new EntitlementState(
                    reported.Id, reported.IsOwned, reported.ExpirationUtc, reported.IsAutoRenewing, EntitlementSource.Restored);
                _verifiedThisSession.Add(reported.Id.Value);
                _isDirty = true;
                RaiseChanged(reported.Id, reported.IsOwned);
            }
        }

        public void Save()
        {
            var data = new EntitlementSaveData();
            foreach (KeyValuePair<string, EntitlementState> pair in _entitlements)
            {
                EntitlementState state = pair.Value;
                data.Entries.Add(new EntitlementSaveData.Entry
                {
                    Id = pair.Key,
                    IsOwned = state.IsOwned,
                    ExpirationUtcTicks = state.ExpirationUtc?.Ticks ?? 0L,
                    IsAutoRenewing = state.IsAutoRenewing,
                    Source = (int)state.Source
                });
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            EntitlementSaveData data = _persistence.Load(SaveKey, SaveVersion, new EntitlementSaveData());
            _entitlements.Clear();
            _verifiedThisSession.Clear();

            if (data.Entries != null)
            {
                foreach (EntitlementSaveData.Entry entry in data.Entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Id))
                    {
                        continue;
                    }

                    // Post-load validation: out-of-range ticks would make new DateTime(...) throw and
                    // abort Initialize; an unknown source value is not a defined enum member. Repair both
                    // in place rather than trusting a user-editable file.
                    DateTime? expiration = null;
                    if (entry.ExpirationUtcTicks > 0)
                    {
                        if (entry.ExpirationUtcTicks <= DateTime.MaxValue.Ticks)
                        {
                            expiration = new DateTime(entry.ExpirationUtcTicks, DateTimeKind.Utc);
                        }
                        else
                        {
                            // Treat an unrepresentable expiration as already expired - never as "forever".
                            expiration = DateTime.MinValue;
                            _log?.Log(LogLevel.Warning, LogCategory, $"Entitlement '{entry.Id}' had an invalid expiration; treated as expired.");
                        }
                    }

                    EntitlementSource source = Enum.IsDefined(typeof(EntitlementSource), entry.Source)
                        ? (EntitlementSource)entry.Source
                        : EntitlementSource.Granted;

                    _entitlements[entry.Id] = new EntitlementState(
                        new EntitlementId(entry.Id), entry.IsOwned, expiration, entry.IsAutoRenewing, source);
                }
            }

            _isDirty = false;
        }

        private void RaiseChanged(EntitlementId id, bool isOwned)
        {
            EntitlementChanged?.Invoke(id, isOwned);
            _events.Publish(new EntitlementChangedEvent(id, isOwned));
        }

        private IReadOnlyList<EntitlementId> BuildOwnedList()
        {
            var owned = new List<EntitlementId>();
            DateTime utcNow = DateTime.UtcNow;

            foreach (KeyValuePair<string, EntitlementState> pair in _entitlements)
            {
                if (pair.Value.IsCurrentlyActive(utcNow))
                {
                    owned.Add(pair.Value.Id);
                }
            }

            return owned;
        }
    }
}
