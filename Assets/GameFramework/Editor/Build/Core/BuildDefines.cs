using System;
using System.Collections.Generic;
using GameFramework.Runtime.Security;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// The one place scripting defines for a framework build are decided. The pipeline adds them
    /// through <c>BuildPlayerOptions.extraScriptingDefines</c>: they apply to that player build only
    /// and are never written to Player Settings, so there is nothing to restore and nothing can leak
    /// into the next build or the Editor.
    ///
    /// Exactly one environment define is added: <c>GAMEFRAMEWORK_ENV_DEVELOPMENT</c>,
    /// <c>GAMEFRAMEWORK_ENV_STAGING</c>, or <c>GAMEFRAMEWORK_ENV_PRODUCTION</c>. It is read at
    /// runtime only through <see cref="BuildEnvironment.Deployment"/>. Everything else a game needs to
    /// branch on should be runtime configuration, not another define.
    /// </summary>
    public static class BuildDefines
    {
        public const string EnvironmentPrefix = "GAMEFRAMEWORK_ENV_";

        /// <summary>Defines that indicate development/cheat/debug code paths, rejected in Production
        /// (in addition to a profile's own <c>ForbiddenProductionDefines</c>). Matching is on the whole
        /// symbol, plus any symbol containing CHEAT or MOCK.</summary>
        private static readonly string[] ForbiddenInProduction = { "DEBUG", "DEVELOPMENT", "DEVELOPMENT_BUILD", "ENABLE_CHEATS" };

        public static string EnvironmentDefine(DeploymentEnvironment environment) =>
            environment == DeploymentEnvironment.Unspecified ? null : EnvironmentPrefix + environment.ToString().ToUpperInvariant();

        /// <summary>The defines the pipeline passes for this build: the environment define plus the
        /// profile's extra defines, trimmed and de-duplicated in a stable order.</summary>
        public static List<string> ForBuild(DeploymentEnvironment environment, IEnumerable<string> extraDefines)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            string environmentDefine = EnvironmentDefine(environment);
            if (environmentDefine != null && seen.Add(environmentDefine))
            {
                result.Add(environmentDefine);
            }

            if (extraDefines != null)
            {
                foreach (string define in extraDefines)
                {
                    string trimmed = define?.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    {
                        result.Add(trimmed);
                    }
                }
            }

            return result;
        }

        /// <summary>Splits Unity's ';'-separated Player Settings define string.</summary>
        public static List<string> Split(string defines)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(defines))
            {
                return result;
            }

            foreach (string part in defines.Split(';'))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        /// <summary>A valid C# conditional-compilation symbol: letter/underscore first, then
        /// letters/digits/underscores.</summary>
        public static bool IsValidSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol) || !(char.IsLetter(symbol[0]) || symbol[0] == '_'))
            {
                return false;
            }

            for (int i = 1; i < symbol.Length; i++)
            {
                char c = symbol[i];
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsForbiddenInProduction(string symbol, IEnumerable<string> additionalForbidden)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return false;
            }

            if (Array.IndexOf(ForbiddenInProduction, symbol) >= 0 ||
                symbol.IndexOf("CHEAT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                symbol.IndexOf("MOCK", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (additionalForbidden != null)
            {
                foreach (string forbidden in additionalForbidden)
                {
                    if (string.Equals(forbidden?.Trim(), symbol, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
