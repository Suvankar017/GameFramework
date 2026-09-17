using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Unlocks
{
    /// <summary>
    /// Default <see cref="IUnlockService"/> — see <c>EconomyService</c>'s remarks for the shared
    /// explicit-Save-Load-with-dirty-flag pattern this mirrors. Unlike Economy/Inventory (whose
    /// definitions are constructor-injected before <see cref="Initialize"/> even runs), unlocks are
    /// registered via <see cref="RegisterUnlock"/> <i>after</i> initialization — a requirement
    /// typically needs another already-initialized service (<see cref="LevelRequirement"/> needs
    /// <see cref="Progression.Experience.IExperienceService"/>, resolved from the registry, which
    /// only succeeds once that service is itself initialized). <see cref="Load"/> therefore cannot
    /// validate persisted ids against <see cref="_entries"/> the way Economy/Inventory validate
    /// against their definitions — see <see cref="Load"/>'s remarks.
    /// </summary>
    public sealed class UnlockService : IUnlockService
    {
        private const string LogCategory = "Unlocks";
        private const string SaveKey = "GameFramework.Unlocks";
        private const int SaveVersion = 1;

        private sealed class Entry
        {
            public UnlockDefinition Definition;
            public IUnlockRequirement Requirement;
        }

        private readonly Dictionary<UnlockId, Entry> _entries = new Dictionary<UnlockId, Entry>();
        private readonly HashSet<UnlockId> _unlocked = new HashSet<UnlockId>();

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

        public void RegisterUnlock(UnlockDefinition definition, IUnlockRequirement requirement)
        {
            Guard.NotNull(definition, nameof(definition));

            UnlockId id = definition.Id;
            if (!id.IsValid)
            {
                throw new System.ArgumentException("UnlockDefinition has no Id assigned.", nameof(definition));
            }

            if (_entries.ContainsKey(id))
            {
                throw new System.InvalidOperationException($"Duplicate unlock id '{id}'.");
            }

            _entries.Add(id, new Entry { Definition = definition, Requirement = requirement });
        }

        public bool IsRegistered(UnlockId id) => _entries.ContainsKey(id);

        public bool IsUnlocked(UnlockId id) => _unlocked.Contains(id);

        public bool CanUnlock(UnlockId id)
        {
            if (_unlocked.Contains(id) || !_entries.TryGetValue(id, out Entry entry))
            {
                return false;
            }

            return entry.Requirement == null || entry.Requirement.IsSatisfied();
        }

        public string GetBlockingReason(UnlockId id)
        {
            if (_unlocked.Contains(id) || !_entries.TryGetValue(id, out Entry entry) || entry.Requirement == null)
            {
                return null;
            }

            return entry.Requirement.IsSatisfied() ? null : entry.Requirement.Describe();
        }

        public UnlockResult TryUnlock(UnlockId id, string reason = null)
        {
            if (!_entries.ContainsKey(id))
            {
                return UnlockResult.InvalidId;
            }

            if (_unlocked.Contains(id))
            {
                return UnlockResult.AlreadyUnlocked;
            }

            if (!CanUnlock(id))
            {
                return UnlockResult.RequirementNotMet;
            }

            ApplyUnlock(id, reason);
            return UnlockResult.Success;
        }

        public UnlockResult ForceUnlock(UnlockId id, string reason = null)
        {
            if (!_entries.ContainsKey(id))
            {
                return UnlockResult.InvalidId;
            }

            if (_unlocked.Contains(id))
            {
                return UnlockResult.AlreadyUnlocked;
            }

            ApplyUnlock(id, reason);
            return UnlockResult.Success;
        }

        public IReadOnlyList<UnlockId> ValidateNoCycles()
        {
            var visited = new HashSet<UnlockId>();
            var onStack = new HashSet<UnlockId>();
            var path = new List<UnlockId>();

            foreach (UnlockId id in _entries.Keys)
            {
                if (visited.Contains(id))
                {
                    continue;
                }

                List<UnlockId> cycle = Visit(id, visited, onStack, path);
                if (cycle != null)
                {
                    _log?.Log(LogLevel.Error, LogCategory,
                        $"Circular unlock dependency detected: {string.Join(" -> ", cycle)} -> {cycle[0]}.");
                    return cycle;
                }
            }

            return System.Array.Empty<UnlockId>();
        }

        public void Save()
        {
            var data = new UnlockSaveData();
            foreach (UnlockId id in _unlocked)
            {
                data.UnlockedIds.Add(id.Value);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            // Deliberately does not filter against _entries: RegisterUnlock necessarily happens
            // after Initialize/Load (a requirement often needs other already-initialized services,
            // e.g. LevelRequirement needs IExperienceService), so _entries is always empty at this
            // point. An id from content that no longer exists just sits unused in _unlocked - see
            // this class's remarks.
            UnlockSaveData data = _persistence.Load(SaveKey, SaveVersion, new UnlockSaveData());
            _unlocked.Clear();

            foreach (string idValue in data.UnlockedIds)
            {
                _unlocked.Add(new UnlockId(idValue));
            }

            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            if (_unlocked.Count == 0)
            {
                return;
            }

            _unlocked.Clear();
            _isDirty = true;
        }

        private void ApplyUnlock(UnlockId id, string reason)
        {
            _unlocked.Add(id);
            _isDirty = true;
            _events.Publish(new UnlockChangedEvent(id, reason));
        }

        private List<UnlockId> Visit(UnlockId id, HashSet<UnlockId> visited, HashSet<UnlockId> onStack, List<UnlockId> path)
        {
            visited.Add(id);
            onStack.Add(id);
            path.Add(id);

            if (_entries.TryGetValue(id, out Entry entry) && entry.Requirement != null)
            {
                foreach (UnlockId next in GetPrerequisiteEdges(entry.Requirement))
                {
                    if (onStack.Contains(next))
                    {
                        int startIndex = path.IndexOf(next);
                        return path.GetRange(startIndex, path.Count - startIndex);
                    }

                    if (!visited.Contains(next))
                    {
                        List<UnlockId> found = Visit(next, visited, onStack, path);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
            }

            onStack.Remove(id);
            path.RemoveAt(path.Count - 1);
            return null;
        }

        private static IEnumerable<UnlockId> GetPrerequisiteEdges(IUnlockRequirement requirement)
        {
            if (requirement is IPrerequisiteRequirement prerequisite)
            {
                yield return prerequisite.TargetId;
            }
            else if (requirement is ICompositeRequirement composite)
            {
                foreach (IUnlockRequirement child in composite.Children)
                {
                    foreach (UnlockId edge in GetPrerequisiteEdges(child))
                    {
                        yield return edge;
                    }
                }
            }
        }
    }
}
