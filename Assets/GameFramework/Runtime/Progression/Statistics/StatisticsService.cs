using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Progression.Statistics
{
    /// <summary>Default <see cref="IStatisticsService"/> — see <c>EconomyService</c>'s remarks for
    /// the shared constructor-injected-definitions / explicit-Save-Load-with-dirty-flag pattern this
    /// mirrors. Values for all three <see cref="StatisticValueType"/>s live in one dictionary keyed
    /// by id (a statistic only ever uses the column matching its own definition), which keeps
    /// lookups O(1) without three separate dictionaries.</summary>
    public sealed class StatisticsService : IStatisticsService
    {
        private const string LogCategory = "Statistics";
        private const string SaveKey = "GameFramework.Progression.Statistics";
        private const int SaveVersion = 1;

        private struct Value
        {
            public int IntValue;
            public float FloatValue;
            public bool BoolValue;
        }

        private readonly Dictionary<StatisticId, StatisticDefinition> _definitions = new Dictionary<StatisticId, StatisticDefinition>();
        private readonly Dictionary<StatisticId, Value> _values = new Dictionary<StatisticId, Value>();

        private IPersistenceService _persistence;
        private IEventService _events;
        private ILoggingService _log;
        private bool _isDirty;

        public StatisticsService(IEnumerable<StatisticDefinition> definitions)
        {
            Guard.NotNull(definitions, nameof(definitions));

            foreach (StatisticDefinition definition in definitions)
            {
                if (definition == null || !definition.Id.IsValid)
                {
                    continue;
                }

                if (_definitions.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException($"Duplicate statistic id '{definition.Id}'.");
                }

                _definitions.Add(definition.Id, definition);
                _values.Add(definition.Id, default);
            }
        }

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

        public bool IsRegistered(StatisticId id) => _definitions.ContainsKey(id);

        public StatisticValueType GetValueType(StatisticId id) =>
            _definitions.TryGetValue(id, out StatisticDefinition definition) ? definition.ValueType : StatisticValueType.Integer;

        public int Get(StatisticId id) => _values.TryGetValue(id, out Value value) ? value.IntValue : 0;

        public bool TryGet(StatisticId id, out int value)
        {
            if (_definitions.TryGetValue(id, out StatisticDefinition definition) &&
                definition.ValueType == StatisticValueType.Integer &&
                _values.TryGetValue(id, out Value stored))
            {
                value = stored.IntValue;
                return true;
            }

            value = 0;
            return false;
        }

        public void Set(StatisticId id, int value, string reason = null)
        {
            if (!RequireIntegerDefinition(id, "Set", out StatisticDefinition definition))
            {
                return;
            }

            int previous = _values[id].IntValue;
            int clamped = ClampInt(definition, value);

            if (definition.IsMonotonic && clamped < previous)
            {
                _log?.Log(LogLevel.Warning, LogCategory,
                    $"Set('{id}', {value}) rejected: statistic is monotonic and {clamped} is lower than current value {previous}.");
                return;
            }

            ApplyInt(id, previous, clamped, reason);
        }

        public void Increment(StatisticId id, int amount = 1, string reason = null)
        {
            if (!RequireIntegerDefinition(id, "Increment", out StatisticDefinition definition))
            {
                return;
            }

            if (amount <= 0)
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Increment('{id}') rejected: non-positive amount {amount}.");
                return;
            }

            int previous = _values[id].IntValue;
            long requested = (long)previous + amount; // widen before clamping to avoid int overflow
            int clamped = ClampInt(definition, requested);

            ApplyInt(id, previous, clamped, reason);
        }

        public void Decrement(StatisticId id, int amount = 1, string reason = null)
        {
            if (!RequireIntegerDefinition(id, "Decrement", out StatisticDefinition definition))
            {
                return;
            }

            if (amount <= 0)
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Decrement('{id}') rejected: non-positive amount {amount}.");
                return;
            }

            if (definition.IsMonotonic)
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Decrement('{id}') rejected: statistic is monotonic.");
                return;
            }

            int previous = _values[id].IntValue;
            long requested = (long)previous - amount;
            int clamped = ClampInt(definition, requested);

            ApplyInt(id, previous, clamped, reason);
        }

        public float GetFloat(StatisticId id) => _values.TryGetValue(id, out Value value) ? value.FloatValue : 0f;

        public void SetFloat(StatisticId id, float value, string reason = null)
        {
            if (!_definitions.TryGetValue(id, out StatisticDefinition definition))
            {
                LogRejected("SetFloat", id, "unregistered statistic");
                return;
            }

            if (definition.ValueType != StatisticValueType.Float)
            {
                LogRejected("SetFloat", id, $"statistic is {definition.ValueType}, not Float");
                return;
            }

            Value stored = _values[id];
            float previous = stored.FloatValue;
            if (Mathf.Approximately(previous, value))
            {
                return;
            }

            stored.FloatValue = value;
            _values[id] = stored;
            _isDirty = true;
            _events.Publish(new StatisticFloatChangedEvent(id, previous, value, reason));
        }

        public bool GetBool(StatisticId id) => _values.TryGetValue(id, out Value value) && value.BoolValue;

        public void SetBool(StatisticId id, bool value, string reason = null)
        {
            if (!_definitions.TryGetValue(id, out StatisticDefinition definition))
            {
                LogRejected("SetBool", id, "unregistered statistic");
                return;
            }

            if (definition.ValueType != StatisticValueType.Boolean)
            {
                LogRejected("SetBool", id, $"statistic is {definition.ValueType}, not Boolean");
                return;
            }

            Value stored = _values[id];
            bool previous = stored.BoolValue;
            if (previous == value)
            {
                return;
            }

            stored.BoolValue = value;
            _values[id] = stored;
            _isDirty = true;
            _events.Publish(new StatisticChangedEvent(id, previous ? 1 : 0, value ? 1 : 0, reason));
        }

        public void Save()
        {
            var data = new StatisticsSaveData();
            foreach (KeyValuePair<StatisticId, StatisticDefinition> pair in _definitions)
            {
                if (!pair.Value.Persistent)
                {
                    continue;
                }

                Value value = _values[pair.Key];
                data.Ids.Add(pair.Key.Value);
                data.IntValues.Add(value.IntValue);
                data.FloatValues.Add(value.FloatValue);
                data.BoolValues.Add(value.BoolValue);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            foreach (StatisticId id in new List<StatisticId>(_values.Keys))
            {
                _values[id] = default;
            }

            StatisticsSaveData data = _persistence.Load(SaveKey, SaveVersion, new StatisticsSaveData());

            for (int i = 0; i < data.Ids.Count; i++)
            {
                var id = new StatisticId(data.Ids[i]);
                if (!_definitions.TryGetValue(id, out StatisticDefinition definition) || !definition.Persistent)
                {
                    continue; // statistic no longer exists (or is session-only) - ignore, not an error
                }

                _values[id] = new Value
                {
                    IntValue = definition.ValueType == StatisticValueType.Integer && i < data.IntValues.Count
                        ? ClampInt(definition, data.IntValues[i])
                        : 0,
                    FloatValue = definition.ValueType == StatisticValueType.Float && i < data.FloatValues.Count
                        ? data.FloatValues[i]
                        : 0f,
                    BoolValue = definition.ValueType == StatisticValueType.Boolean && i < data.BoolValues.Count && data.BoolValues[i]
                };
            }

            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            foreach (StatisticId id in new List<StatisticId>(_values.Keys))
            {
                _values[id] = default;
            }

            _isDirty = true;
        }

        private bool RequireIntegerDefinition(StatisticId id, string operation, out StatisticDefinition definition)
        {
            if (!_definitions.TryGetValue(id, out definition))
            {
                LogRejected(operation, id, "unregistered statistic");
                return false;
            }

            if (definition.ValueType != StatisticValueType.Integer)
            {
                LogRejected(operation, id, $"statistic is {definition.ValueType}, not Integer");
                return false;
            }

            return true;
        }

        private void ApplyInt(StatisticId id, int previous, int newValue, string reason)
        {
            if (newValue == previous)
            {
                return;
            }

            Value stored = _values[id];
            stored.IntValue = newValue;
            _values[id] = stored;
            _isDirty = true;
            _events.Publish(new StatisticChangedEvent(id, previous, newValue, reason));
        }

        private static int ClampInt(StatisticDefinition definition, long value)
        {
            int min = definition.MinValue;
            int max = definition.MaxValue;

            if (max > 0 && value > max)
            {
                value = max;
            }

            if (value < min)
            {
                value = min;
            }

            return (int)value;
        }

        private void LogRejected(string operation, StatisticId id, string detail)
        {
            _log?.Log(LogLevel.Warning, LogCategory, $"{operation}('{id}') rejected: {detail}.");
        }
    }
}
