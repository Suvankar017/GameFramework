using System.Reflection;
using GameFramework.RemoteConfig.LiveOps;
using GameFramework.RemoteConfig.Providers.Mock;
using UnityEngine;

namespace GameFramework.RemoteConfig.Tests
{
    /// <summary>See <c>Monetization.Tests.TestDefinitions</c>'s remarks - same reflection-based
    /// private-field population, since none of these authoring types expose a public constructor.</summary>
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static RemoteConfigDefinition BoolDefinition(string key, bool defaultValue, string domain = null) =>
            Definition(key, RemoteConfigTypedValue.FromBool(defaultValue), domain);

        public static RemoteConfigDefinition IntDefinition(string key, int defaultValue, bool hasRange = false, double min = 0, double max = 0, string domain = null)
        {
            RemoteConfigDefinition definition = Definition(key, RemoteConfigTypedValue.FromInt(defaultValue), domain);
            SetField(definition, "_hasRange", hasRange);
            SetField(definition, "_minValue", min);
            SetField(definition, "_maxValue", max);
            return definition;
        }

        public static RemoteConfigDefinition FloatDefinition(string key, float defaultValue, string domain = null) =>
            Definition(key, RemoteConfigTypedValue.FromFloat(defaultValue), domain);

        public static RemoteConfigDefinition StringDefinition(string key, string defaultValue, string domain = null) =>
            Definition(key, RemoteConfigTypedValue.FromString(defaultValue), domain);

        private static RemoteConfigDefinition Definition(string key, RemoteConfigTypedValue value, string domain)
        {
            var definition = new RemoteConfigDefinition();
            SetField(definition, "_key", key);
            SetField(definition, "_defaultValue", value);
            SetField(definition, "_domain", domain);
            return definition;
        }

        public static RemoteConfigConfiguration Configuration(
            RemoteConfigDefinition[] definitions,
            int supportedSchemaVersion = 1,
            bool fetchOnStartup = false,
            bool autoFetchOnResume = false,
            float fetchTimeoutSeconds = 5f,
            float staleThresholdSeconds = 3600f,
            float cacheExpirationSeconds = 604800f,
            bool useStaleCache = true,
            bool cacheEnabled = true,
            RemoteConfigEnvironment environment = RemoteConfigEnvironment.Development)
        {
            var configuration = ScriptableObject.CreateInstance<RemoteConfigConfiguration>();
            SetField(configuration, "_definitions", definitions);
            SetField(configuration, "_environment", environment);
            SetField(configuration, "_supportedSchemaVersion", supportedSchemaVersion);
            SetField(configuration, "_fetchOnStartup", fetchOnStartup);
            SetField(configuration, "_autoFetchOnResume", autoFetchOnResume);
            SetField(configuration, "_fetchTimeoutSeconds", fetchTimeoutSeconds);
            SetField(configuration, "_staleThresholdSeconds", staleThresholdSeconds);
            SetField(configuration, "_cacheExpirationSeconds", cacheExpirationSeconds);
            SetField(configuration, "_useStaleCache", useStaleCache);
            SetField(configuration, "_cacheEnabled", cacheEnabled);
            return configuration;
        }

        public static LiveEventDefinition LiveEvent(string id, string startUtc, string endUtc, bool enabled = true, string configKeyPrefix = null)
        {
            var definition = new LiveEventDefinition();
            SetField(definition, "_id", id);
            SetField(definition, "_enabled", enabled);
            SetField(definition, "_startTimeUtc", startUtc);
            SetField(definition, "_endTimeUtc", endUtc);
            SetField(definition, "_configKeyPrefix", configKeyPrefix);
            return definition;
        }

        public static LiveOpsConfiguration LiveOpsConfig(LiveEventDefinition[] events, float pollIntervalSeconds = 1000f)
        {
            var configuration = ScriptableObject.CreateInstance<LiveOpsConfiguration>();
            SetField(configuration, "_events", events);
            SetField(configuration, "_pollIntervalSeconds", pollIntervalSeconds);
            return configuration;
        }

        public static MockRemoteConfigEntry MockEntry(string key, RemoteConfigTypedValue value)
        {
            var entry = new MockRemoteConfigEntry();
            SetField(entry, "_key", key);
            SetField(entry, "_value", value);
            return entry;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
