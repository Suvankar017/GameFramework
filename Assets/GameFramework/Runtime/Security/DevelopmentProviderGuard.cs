using System;
using UnityEngine;

namespace GameFramework.Runtime.Security
{
    /// <summary>
    /// Selects between a development-only provider (a Mock ad/purchase/analytics/remote-config/
    /// notification provider) and its production-safe fallback (a NoOp provider), refusing the
    /// development one in a non-development build even if a serialized inspector toggle asks for it.
    ///
    /// This exists because a bootstrapper's "use mock provider" checkbox is serialized scene/prefab
    /// data: nothing stops it from being left ticked when a release build is made. For monetization in
    /// particular, a mock purchase provider in a release build would grant real entitlements for free.
    /// The refusal is logged as an error (it is a build-configuration mistake a developer must see),
    /// and the fallback is used so the game still starts.
    /// </summary>
    public static class DevelopmentProviderGuard
    {
        /// <summary>Uses <see cref="BuildEnvironment.IsDevelopmentBuild"/>.</summary>
        public static T Select<T>(bool useDevelopmentProvider, Func<T> createDevelopmentProvider, Func<T> createFallbackProvider, string context)
            where T : class =>
            Select(useDevelopmentProvider, BuildEnvironment.IsDevelopmentBuild, createDevelopmentProvider, createFallbackProvider, context);

        /// <summary>Explicit-environment overload - exists so the release-build refusal path is unit
        /// testable from inside the Editor, where <see cref="BuildEnvironment.IsDevelopmentBuild"/> is
        /// always true.</summary>
        public static T Select<T>(
            bool useDevelopmentProvider,
            bool isDevelopmentBuild,
            Func<T> createDevelopmentProvider,
            Func<T> createFallbackProvider,
            string context)
            where T : class
        {
            if (createDevelopmentProvider == null)
            {
                throw new ArgumentNullException(nameof(createDevelopmentProvider));
            }

            if (createFallbackProvider == null)
            {
                throw new ArgumentNullException(nameof(createFallbackProvider));
            }

            if (!useDevelopmentProvider)
            {
                return createFallbackProvider();
            }

            if (isDevelopmentBuild)
            {
                return createDevelopmentProvider();
            }

            // Debug.LogError, not the Log facade: this runs from a bootstrapper's RegisterServices,
            // before LoggingService.Initialize has bound the facade, so Log.Error would be dropped.
            Debug.LogError(
                $"[Security] {context}: a development-only (mock) provider was selected in a non-development build. " +
                "It has been replaced with the production-safe fallback provider. Disable the mock toggle " +
                "or install a real provider adapter before shipping.");
            return createFallbackProvider();
        }
    }
}
