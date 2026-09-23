using System;
using System.Collections.Generic;
using GameFramework.Analytics;
using GameFramework.Editor.Security;
using GameFramework.Monetization;
using GameFramework.Monetization.Purchases;
using GameFramework.RemoteConfig;
using GameFramework.Runtime.Security;
using UnityEditor;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// Production/staging safety. It rejects:
    /// <list type="bullet">
    /// <item>development/cheat/mock scripting defines;</item>
    /// <item>pipeline-managed environment defines set by hand in Player Settings;</item>
    /// <item>development-only mock providers left enabled on a bootstrapper in the build scenes;</item>
    /// <item>committed credentials (the Phase 19 preflight scan);</item>
    /// <item>a dirty Git tree when the profile requires a clean one.</item>
    /// </list>
    /// A mock toggle would already be refused at runtime by Phase 19's
    /// <c>DevelopmentProviderGuard</c>, but a release that silently runs on NoOp providers is still a
    /// broken release. That is why the build fails early instead.
    /// </summary>
    public sealed class ReleaseSafetyValidator : IBuildValidator
    {
        /// <summary>Serialized mock-provider toggles on the framework's bootstrappers.</summary>
        internal static readonly string[] MockToggleFields =
        {
            "_useMockProviders", "_useMockProvider", "_useMockAnalyticsProvider", "_useMockCrashReportingProvider"
        };

        public void Validate(BuildContext context, BuildValidationReport report)
        {
            ValidateDefines(context, report);
            ValidateMockProviders(context, report);

            if (context.IsReleaseEnvironment)
            {
                ValidateSecrets(context, report);
            }

            ValidateGit(context, report);
        }

        private static void ValidateDefines(BuildContext context, BuildValidationReport report)
        {
            bool clean = true;
            foreach (string define in context.ProjectDefines)
            {
                if (define.StartsWith(BuildDefines.EnvironmentPrefix, StringComparison.Ordinal))
                {
                    report.Error("Defines.EnvironmentManaged",
                        $"Player Settings define '{define}' is managed by the build pipeline and must not be set by hand.",
                        "Remove it from Player Settings > Scripting Define Symbols; the profile's environment adds it per build.");
                    clean = false;
                }
            }

            if (context.IsProduction)
            {
                var all = new List<string>(context.ProjectDefines);
                all.AddRange(context.PipelineDefines);
                foreach (string define in all)
                {
                    if (BuildDefines.IsForbiddenInProduction(define, context.Profile.ForbiddenProductionDefines))
                    {
                        report.Error("Defines.Production", $"Scripting define '{define}' is not allowed in a Production build.",
                            "Remove it from Player Settings or the profile's extra defines.");
                        clean = false;
                    }
                }
            }

            if (clean)
            {
                report.Pass("Defines", $"Build defines: {string.Join(";", context.PipelineDefines)}.");
            }
        }

        private static void ValidateMockProviders(BuildContext context, BuildValidationReport report)
        {
            var enabled = new List<string>();
            foreach (ScannedComponent component in context.SceneScan.Components)
            {
                foreach (string field in MockToggleFields)
                {
                    string value = component.GetField(field);

                    // MonetizationBootstrapper's toggle defaults to on (Phase 19); a scene serialized
                    // before the field existed has no value but still selects the mocks.
                    bool implicitDefault = value == null && field == "_useMockProviders" && component.Is<MonetizationBootstrapper>();
                    if (value == "1" || implicitDefault)
                    {
                        enabled.Add($"{component.ScriptType?.Name}.{field} in {component.SourcePath}{(implicitDefault ? " (default)" : string.Empty)}");
                    }
                }
            }

            foreach ((string source, string propertyPath, string value) in context.SceneScan.PrefabOverrides)
            {
                if (value == "1" && Array.IndexOf(MockToggleFields, propertyPath) >= 0)
                {
                    enabled.Add($"prefab override {propertyPath} in {source}");
                }
            }

            if (enabled.Count == 0)
            {
                report.Pass("Providers.Mock", "No mock providers enabled in the build scenes.");
                return;
            }

            string list = string.Join("; ", enabled);
            if (context.IsReleaseEnvironment)
            {
                report.ErrorOrWarning(context.IsProduction, "Providers.Mock",
                    $"Development-only mock providers are enabled: {list}.",
                    "Disable the mock toggles and register real provider adapters for this environment.");
            }
            else
            {
                report.Info("Providers.Mock", $"Mock providers enabled (allowed for Development): {list}.");
            }
        }

        private static void ValidateSecrets(BuildContext context, BuildValidationReport report)
        {
            List<PreflightIssue> findings = FrameworkPreflight.ScanForSecrets();
            if (findings.Count == 0)
            {
                report.Pass("Security.Secrets", "No credential-shaped strings found in Assets/ or ProjectSettings/ (heuristic scan).");
                return;
            }

            foreach (PreflightIssue finding in findings)
            {
                // Location and pattern name only - the preflight never includes the value.
                report.Error("Security.Secrets", $"{finding.Source}: {finding.Message}", finding.SuggestedFix);
            }
        }

        private static void ValidateGit(BuildContext context, BuildValidationReport report)
        {
            GitInfo git = context.Git;
            if (!git.Available)
            {
                report.ErrorOrWarning(context.Profile.RequireCleanGitTree, "Git.Metadata",
                    $"Git metadata unavailable ({git.FailureReason}); commit/branch will be recorded as '{GitInfo.Unknown}'.");
                return;
            }

            if (git.IsDirty)
            {
                if (context.Profile.RequireCleanGitTree)
                {
                    report.Error("Git.Clean", "The working tree has uncommitted changes and this profile requires a clean tree for reproducible releases.",
                        "Commit or stash your changes, or untick 'Require Clean Git Tree'.");
                }
                else if (context.IsProduction)
                {
                    report.Warning("Git.Clean", "Production build from a working tree with uncommitted changes; the recorded commit does not fully describe it.");
                }
                else
                {
                    report.Info("Git.Clean", "Working tree has uncommitted changes.");
                }

                return;
            }

            report.Pass("Git.Clean", $"Clean working tree at {git.Branch}@{Short(git.Commit)}.");
        }

        private static string Short(string commit) => commit.Length > 12 ? commit.Substring(0, 12) : commit;
    }

    /// <summary>
    /// Checks the framework systems that are actually present in the build scenes against the
    /// selected environment:
    /// <list type="bullet">
    /// <item>Remote Config: the configuration asset's environment matches, so no cross-environment
    /// cache or endpoints are used.</item>
    /// <item>Monetization: the product catalog has store ids for the target, and no Google static
    /// test SKUs in Production.</item>
    /// <item>Analytics: a configuration asset is assigned.</item>
    /// </list>
    /// Everything runs offline; no network or store dashboard is ever queried.
    /// </summary>
    public sealed class FrameworkIntegrationValidator : IBuildValidator
    {
        private static readonly string[] GooglePlayTestProductIds =
        {
            "android.test.purchased", "android.test.canceled", "android.test.refunded", "android.test.item_unavailable"
        };

        public void Validate(BuildContext context, BuildValidationReport report)
        {
            foreach (ScannedComponent component in context.SceneScan.OfType<RemoteConfigBootstrapper>())
            {
                ValidateRemoteConfig(context, report, component);
            }

            foreach (ScannedComponent component in context.SceneScan.OfType<MonetizationBootstrapper>())
            {
                ValidateMonetization(context, report, component);
            }

            foreach (ScannedComponent component in context.SceneScan.OfType<AnalyticsBootstrapper>())
            {
                if (component.GetReferenceGuid("_analyticsConfiguration") == null)
                {
                    report.ErrorOrWarning(context.IsProduction, "Analytics.Configuration",
                        $"{component.ScriptType.Name} in {component.SourcePath} has no AnalyticsConfiguration assigned; runtime defaults will be used.");
                }
                else
                {
                    report.Pass("Analytics.Configuration", $"{component.ScriptType.Name} has an AnalyticsConfiguration.");
                }
            }
        }

        public static RemoteConfigEnvironment? MapEnvironment(DeploymentEnvironment environment)
        {
            switch (environment)
            {
                case DeploymentEnvironment.Development: return RemoteConfigEnvironment.Development;
                case DeploymentEnvironment.Staging: return RemoteConfigEnvironment.Staging;
                case DeploymentEnvironment.Production: return RemoteConfigEnvironment.Production;
                default: return null;
            }
        }

        public static bool IsGooglePlayTestProductId(string productId) =>
            !string.IsNullOrEmpty(productId) && Array.IndexOf(GooglePlayTestProductIds, productId.Trim()) >= 0;

        private static void ValidateRemoteConfig(BuildContext context, BuildValidationReport report, ScannedComponent component)
        {
            RemoteConfigEnvironment? expected = MapEnvironment(context.Environment);
            var configuration = Load<RemoteConfigConfiguration>(component.GetReferenceGuid("_remoteConfigConfiguration"));

            // No asset means runtime defaults, whose environment is Development.
            RemoteConfigEnvironment actual = configuration != null ? configuration.Environment : RemoteConfigEnvironment.Development;
            string source = configuration != null ? AssetDatabase.GetAssetPath(configuration) : "runtime default (no asset assigned)";

            if (expected.HasValue && actual != expected.Value)
            {
                report.ErrorOrWarning(context.IsReleaseEnvironment, "RemoteConfig.Environment",
                    $"Remote config environment is {actual} ({source}) but the build environment is {context.Environment}.",
                    "Assign a RemoteConfigConfiguration for this environment (a separate asset per environment keeps caches and endpoints apart).");
                return;
            }

            report.Pass("RemoteConfig.Environment", $"Remote config environment {actual} matches.");
        }

        private static void ValidateMonetization(BuildContext context, BuildValidationReport report, ScannedComponent component)
        {
            var catalog = Load<ProductCatalog>(component.GetReferenceGuid("_productCatalog"));
            if (catalog == null)
            {
                report.ErrorOrWarning(context.IsProduction, "Monetization.Catalog", $"{component.ScriptType.Name} in {component.SourcePath} has no ProductCatalog assigned.");
                return;
            }

            bool isMobile = context.Target == BuildTarget.Android || context.Target == BuildTarget.iOS;
            bool ok = true;
            foreach (ProductDefinition product in catalog.Products)
            {
                if (product == null || !isMobile)
                {
                    continue;
                }

                string storeId = context.Target == BuildTarget.Android ? product.AndroidProductId : product.IosProductId;
                if (string.IsNullOrWhiteSpace(storeId))
                {
                    report.ErrorOrWarning(context.IsProduction, "Monetization.ProductIds",
                        $"Product '{product.Id}' has no {ArtifactNaming.PlatformName(context.Target)} store id in {AssetDatabase.GetAssetPath(catalog)}.");
                    ok = false;
                }
                else if (context.IsProduction && IsGooglePlayTestProductId(storeId))
                {
                    report.Error("Monetization.ProductIds", $"Product '{product.Id}' uses Google Play's static test id '{storeId}' in a Production build.");
                    ok = false;
                }
            }

            if (ok)
            {
                report.Pass("Monetization.ProductIds", isMobile
                    ? $"All {catalog.Products.Length} product(s) have {ArtifactNaming.PlatformName(context.Target)} store ids."
                    : "Non-mobile target: store ids not required.");
            }
        }

        private static T Load<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
