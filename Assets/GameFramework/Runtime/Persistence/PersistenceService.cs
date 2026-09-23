using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;

namespace GameFramework.Runtime.Persistence
{
    /// <summary>
    /// Default <see cref="IPersistenceService"/>.
    ///
    /// <para><b>Save pipeline:</b> validate input → serialize → reject an empty payload → compute a
    /// SHA-256 checksum (<see cref="SaveIntegrity"/>) → hand the envelope to
    /// <see cref="IPersistenceStorage.WriteText"/>, which is responsible for making the write atomic
    /// (see <see cref="FilePersistenceStorage"/>).</para>
    ///
    /// <para><b>Load pipeline:</b> read → parse envelope → verify checksum → reject a newer-than-supported
    /// version → migrate step by step → deserialize → reject null. Any failure preserves the raw text
    /// under <c>"{key}.corrupt"</c> and reports a <see cref="PersistenceLoadStatus"/>. Semantic
    /// validation of the loaded object (ranges, references) remains the owning system's job, since only
    /// it knows its own data contract (e.g. <c>PlayerData.IPlayerDataSection.Validate</c>) - this layer
    /// never invents game-specific limits.</para>
    /// </summary>
    public sealed class PersistenceService : IPersistenceService
    {
        private const string LogCategory = "Persistence";
        private const string CorruptSuffix = ".corrupt";

        private readonly IPersistenceStorage _storage;
        private readonly IPersistenceSerializer _serializer;
        private readonly Dictionary<string, Dictionary<int, ISaveMigration>> _migrations =
            new Dictionary<string, Dictionary<int, ISaveMigration>>();

        private ILoggingService _log;

        public PersistenceService(IPersistenceStorage storage, IPersistenceSerializer serializer)
        {
            _storage = Guard.NotNull(storage, nameof(storage));
            _serializer = Guard.NotNull(serializer, nameof(serializer));
        }

        public void Initialize(IServiceRegistry registry)
        {
            registry.TryGet(out _log);
        }

        public void Shutdown()
        {
            _migrations.Clear();
        }

        public bool Exists(string key) => _storage.Exists(key);

        public void Delete(string key) => _storage.Delete(key);

        public void Save<TData>(string key, TData data, int version)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), $"Cannot save null data under '{key}'; use Delete to clear a key.");
            }

            string payload = _serializer.Serialize(data);
            if (string.IsNullOrEmpty(payload))
            {
                // Validate before writing: never replace a good save with an empty payload.
                throw new InvalidOperationException(
                    $"Serializing '{typeof(TData).Name}' for '{key}' produced no data; the existing save was left untouched.");
            }

            var envelope = new SaveEnvelope
            {
                Version = version,
                Payload = payload,
                Checksum = SaveIntegrity.Compute(version, payload)
            };

            _storage.WriteText(key, _serializer.Serialize(envelope));
        }

        public TData Load<TData>(string key, int currentVersion, TData defaultValue)
        {
            return TryLoad(key, currentVersion, out TData data).IsSuccess() ? data : defaultValue;
        }

        public PersistenceLoadStatus TryLoad<TData>(string key, int currentVersion, out TData data)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            data = default;

            string rawText;
            try
            {
                if (!_storage.Exists(key))
                {
                    return PersistenceLoadStatus.Missing;
                }

                rawText = _storage.ReadText(key);
            }
            catch (Exception exception)
            {
                _log?.Log(LogLevel.Error, LogCategory, $"Could not read '{key}': {exception.GetType().Name}: {exception.Message}");
                return PersistenceLoadStatus.Unreadable;
            }

            if (rawText == null)
            {
                return PersistenceLoadStatus.Missing;
            }

            PersistenceLoadStatus status = Decode(key, rawText, currentVersion, out data, out string failureDetail);
            if (status.IsFailure())
            {
                // Never log raw save contents - only the key, the outcome, and a technical reason.
                _log?.Log(LogLevel.Error, LogCategory,
                    $"Load of '{key}' failed ({status}): {failureDetail} Raw data preserved under '{key}{CorruptSuffix}'.");
                PreserveRawData(key, rawText);
                data = default;
            }

            return status;
        }

        public void RegisterMigration(string key, ISaveMigration migration)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNull(migration, nameof(migration));

            if (!_migrations.TryGetValue(key, out Dictionary<int, ISaveMigration> stepsForKey))
            {
                stepsForKey = new Dictionary<int, ISaveMigration>();
                _migrations.Add(key, stepsForKey);
            }

            if (stepsForKey.ContainsKey(migration.FromVersion))
            {
                throw new InvalidOperationException(
                    $"A migration from version {migration.FromVersion} is already registered for '{key}'.");
            }

            stepsForKey.Add(migration.FromVersion, migration);
        }

        private PersistenceLoadStatus Decode<TData>(string key, string rawText, int currentVersion, out TData data, out string failureDetail)
        {
            data = default;

            SaveEnvelope envelope;
            try
            {
                envelope = _serializer.Deserialize<SaveEnvelope>(rawText);
            }
            catch (Exception exception)
            {
                failureDetail = $"Envelope could not be parsed ({exception.GetType().Name}).";
                return PersistenceLoadStatus.Corrupted;
            }

            if (envelope == null || envelope.Payload == null)
            {
                failureDetail = "Envelope is empty or has no payload.";
                return PersistenceLoadStatus.Corrupted;
            }

            if (!SaveIntegrity.Verify(envelope))
            {
                failureDetail = "Checksum does not match contents (truncated or modified data).";
                return PersistenceLoadStatus.Corrupted;
            }

            if (envelope.Version < 0)
            {
                failureDetail = $"Stored version {envelope.Version} is invalid.";
                return PersistenceLoadStatus.Corrupted;
            }

            if (envelope.Version > currentVersion)
            {
                failureDetail = $"Stored version {envelope.Version} is newer than the supported version {currentVersion}.";
                return PersistenceLoadStatus.UnsupportedVersion;
            }

            bool migrated = false;
            string payload = envelope.Payload;
            int version = envelope.Version;

            while (version < currentVersion)
            {
                if (!TryGetMigration(key, version, out ISaveMigration migration))
                {
                    failureDetail = $"No migration registered from version {version} to {currentVersion}.";
                    return PersistenceLoadStatus.MigrationMissing;
                }

                // A step that doesn't strictly advance (or overshoots the target) would loop forever or
                // produce data the caller can't interpret - reject it rather than trust the chain.
                if (migration.ToVersion <= version || migration.ToVersion > currentVersion)
                {
                    failureDetail = $"Migration {migration.FromVersion}->{migration.ToVersion} does not advance toward version {currentVersion}.";
                    return PersistenceLoadStatus.MigrationFailed;
                }

                string next;
                try
                {
                    next = migration.Migrate(payload);
                }
                catch (Exception exception)
                {
                    failureDetail = $"Migration {migration.FromVersion}->{migration.ToVersion} threw {exception.GetType().Name}: {exception.Message}";
                    return PersistenceLoadStatus.MigrationFailed;
                }

                if (next == null)
                {
                    failureDetail = $"Migration {migration.FromVersion}->{migration.ToVersion} returned null.";
                    return PersistenceLoadStatus.MigrationFailed;
                }

                payload = next;
                version = migration.ToVersion;
                migrated = true;
            }

            try
            {
                data = _serializer.Deserialize<TData>(payload);
            }
            catch (Exception exception)
            {
                failureDetail = $"Payload could not be deserialized as {typeof(TData).Name} ({exception.GetType().Name}).";
                return PersistenceLoadStatus.Corrupted;
            }

            if (data == null)
            {
                failureDetail = $"Payload deserialized to null as {typeof(TData).Name}.";
                return PersistenceLoadStatus.Corrupted;
            }

            failureDetail = null;
            return migrated ? PersistenceLoadStatus.Migrated : PersistenceLoadStatus.Loaded;
        }

        private bool TryGetMigration(string key, int fromVersion, out ISaveMigration migration)
        {
            if (_migrations.TryGetValue(key, out Dictionary<int, ISaveMigration> stepsForKey))
            {
                return stepsForKey.TryGetValue(fromVersion, out migration);
            }

            migration = null;
            return false;
        }

        private void PreserveRawData(string key, string rawText)
        {
            try
            {
                _storage.WriteText(key + CorruptSuffix, rawText);
            }
            catch (Exception exception)
            {
                // Preservation is best-effort diagnostics; failing to write it must not turn a
                // recoverable load failure into a crash. Reported, not swallowed.
                _log?.Log(LogLevel.Error, LogCategory,
                    $"Could not preserve corrupt data for '{key}': {exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}
