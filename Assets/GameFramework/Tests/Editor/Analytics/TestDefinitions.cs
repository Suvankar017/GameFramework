using System.Reflection;
using GameFramework.Analytics.Diagnostics;
using UnityEngine;

namespace GameFramework.Analytics.Tests
{
    /// <summary>See <c>GameFramework.Monetization.Tests.TestDefinitions</c>'s remarks - same
    /// reflection-based private-field population, since neither configuration ScriptableObject
    /// exposes a public setter (by design).</summary>
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static AnalyticsConfiguration AnalyticsConfig(
            bool enabledByDefault = true,
            bool consentRequired = true,
            ConsentPolicy consentPolicy = ConsentPolicy.BufferUntilDecided,
            int eventQueueCapacity = 100,
            int maxParametersPerEvent = 25,
            int maxEventNameLength = 40,
            int maxParameterKeyLength = 40,
            int maxParameterValueLength = 100,
            float sessionTimeoutSeconds = 1800f)
        {
            var config = ScriptableObject.CreateInstance<AnalyticsConfiguration>();
            SetField(config, "_enabledByDefault", enabledByDefault);
            SetField(config, "_consentRequired", consentRequired);
            SetField(config, "_consentPolicy", consentPolicy);
            SetField(config, "_eventQueueCapacity", eventQueueCapacity);
            SetField(config, "_maxParametersPerEvent", maxParametersPerEvent);
            SetField(config, "_maxEventNameLength", maxEventNameLength);
            SetField(config, "_maxParameterKeyLength", maxParameterKeyLength);
            SetField(config, "_maxParameterValueLength", maxParameterValueLength);
            SetField(config, "_sessionTimeoutSeconds", sessionTimeoutSeconds);
            return config;
        }

        public static DiagnosticsConfiguration DiagnosticsConfig(
            bool enabled = true, int breadcrumbCapacity = 50, bool captureUnhandledExceptions = false)
        {
            var config = ScriptableObject.CreateInstance<DiagnosticsConfiguration>();
            SetField(config, "_enabled", enabled);
            SetField(config, "_breadcrumbCapacity", breadcrumbCapacity);
            SetField(config, "_captureUnhandledExceptions", captureUnhandledExceptions);
            return config;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
