using System;
using System.Collections.Generic;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// An immutable, consistent set of resolved configuration values - see CLAUDE.md's Phase 17
    /// brief, sections 13/77. Once constructed, a snapshot never changes; a new activation produces a
    /// brand-new instance rather than mutating this one, so a consumer holding a reference to one
    /// never sees a value change out from under it mid-read.
    ///
    /// <see cref="Values"/> always contains every declared <see cref="RemoteConfigDefinition"/>'s
    /// current value (its local default, unless overridden by a cached/fetched payload) plus any
    /// additional ad hoc keys that payload happened to include - see
    /// <see cref="RemoteConfigService"/>'s remarks on precedence layering.
    /// </summary>
    public sealed class RemoteConfigSnapshot
    {
        /// <summary>0 for the defaults-only snapshot built before any successful cache/remote
        /// activation; the fetched/cached payload's own version thereafter.</summary>
        public int Version { get; }

        public int SchemaVersion { get; }
        public RemoteConfigEnvironment Environment { get; }

        /// <summary>When the underlying data was originally fetched/cached - not when it was
        /// activated in this process (see <see cref="ActivatedAtUtc"/>).</summary>
        public DateTime FetchedAtUtc { get; }

        /// <summary>When this snapshot became the active one in this process.</summary>
        public DateTime ActivatedAtUtc { get; }

        public IReadOnlyDictionary<string, object> Values { get; }

        public RemoteConfigSnapshot(
            int version,
            int schemaVersion,
            RemoteConfigEnvironment environment,
            DateTime fetchedAtUtc,
            DateTime activatedAtUtc,
            IReadOnlyDictionary<string, object> values)
        {
            Version = version;
            SchemaVersion = schemaVersion;
            Environment = environment;
            FetchedAtUtc = fetchedAtUtc;
            ActivatedAtUtc = activatedAtUtc;
            Values = values ?? new Dictionary<string, object>(0);
        }

        /// <summary>Builds the defaults-only snapshot used before any cache/remote data has ever been
        /// activated - see CLAUDE.md's Phase 17 brief, section 7 (the game must remain playable with
        /// nothing but local defaults).</summary>
        public static RemoteConfigSnapshot FromDefaults(IReadOnlyList<RemoteConfigDefinition> definitions, RemoteConfigEnvironment environment, int schemaVersion)
        {
            var values = new Dictionary<string, object>(definitions?.Count ?? 0, StringComparer.Ordinal);
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    RemoteConfigDefinition definition = definitions[i];
                    if (definition != null && definition.IsKeyValid)
                    {
                        values[definition.Key] = definition.DefaultValueBoxed;
                    }
                }
            }

            DateTime now = DateTime.UtcNow;
            return new RemoteConfigSnapshot(0, schemaVersion, environment, now, now, values);
        }
    }
}
