using System;
using System.Collections.Generic;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// A route path template, e.g. <c>"daily-reward"</c> or <c>"shop/{itemId}"</c> - see CLAUDE.md's
    /// Phase 18 brief, section 18. Matches on <see cref="DeepLink.Path"/> only (never
    /// <see cref="DeepLink.Scheme"/>/<see cref="DeepLink.Host"/>) so the same route matches whether it
    /// arrived as <c>mygame://daily-reward</c> or <c>https://example.com/game/daily-reward</c> once a
    /// game registers the pattern accordingly - see <see cref="DeepLinkParser"/>'s remarks on why both
    /// shapes normalize into the same <see cref="DeepLink.Path"/> representation.
    /// </summary>
    public sealed class DeepLinkRoutePattern
    {
        private readonly string[] _segments;

        public string Pattern { get; }

        public DeepLinkRoutePattern(string pattern)
        {
            Pattern = pattern ?? string.Empty;
            _segments = Pattern.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>True if <paramref name="link"/>'s path has the same segment count and every
        /// literal segment matches exactly (case-insensitive); a <c>{name}</c> segment always matches
        /// and is captured into <paramref name="pathParameters"/>.</summary>
        public bool TryMatch(DeepLink link, out IReadOnlyDictionary<string, string> pathParameters)
        {
            string[] actual = (link.Path ?? string.Empty).Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (actual.Length != _segments.Length)
            {
                pathParameters = null;
                return false;
            }

            Dictionary<string, string> parameters = null;

            for (int i = 0; i < _segments.Length; i++)
            {
                string segment = _segments[i];

                if (segment.Length > 2 && segment[0] == '{' && segment[segment.Length - 1] == '}')
                {
                    parameters ??= new Dictionary<string, string>(StringComparer.Ordinal);
                    parameters[segment.Substring(1, segment.Length - 2)] = Uri.UnescapeDataString(actual[i]);
                }
                else if (!string.Equals(segment, actual[i], StringComparison.OrdinalIgnoreCase))
                {
                    pathParameters = null;
                    return false;
                }
            }

            pathParameters = parameters ?? EmptyParameters;
            return true;
        }

        private static readonly Dictionary<string, string> EmptyParameters = new Dictionary<string, string>(0);
    }
}
