using System;
using System.Collections.Generic;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// Structural limits <see cref="DeepLinkService"/> applies to every inbound URI before parsing or
    /// routing it - every deep link is untrusted external input (CLAUDE.md's Phase 19 section, "Deep-link
    /// validation"). These checks are about shape, not meaning: whether a route/parameter value is
    /// <i>acceptable</i> is still each <see cref="IDeepLinkHandler"/>'s decision, and a deep link can only
    /// ever <i>request</i> an action - it must never grant currency, entitlements, purchases, or progress
    /// by itself.
    ///
    /// The defaults are deliberately generous (a real link is far below all of them) so they reject only
    /// malformed or hostile input, never a legitimate link.
    /// </summary>
    public sealed class DeepLinkValidationOptions
    {
        /// <summary>Default maximum raw URI length - 4096 characters, above the ~2000-character limit
        /// most platforms and browsers practically support for a URL.</summary>
        public const int DefaultMaxUriLength = 4096;

        /// <summary>Default maximum number of query parameters.</summary>
        public const int DefaultMaxQueryParameters = 64;

        /// <summary>Default maximum length of one decoded query key or value.</summary>
        public const int DefaultMaxQueryValueLength = 1024;

        private readonly HashSet<string> _allowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public DeepLinkValidationOptions(
            int maxUriLength = DefaultMaxUriLength,
            int maxQueryParameters = DefaultMaxQueryParameters,
            int maxQueryValueLength = DefaultMaxQueryValueLength,
            IEnumerable<string> allowedSchemes = null)
        {
            MaxUriLength = maxUriLength > 0 ? maxUriLength : DefaultMaxUriLength;
            MaxQueryParameters = maxQueryParameters > 0 ? maxQueryParameters : DefaultMaxQueryParameters;
            MaxQueryValueLength = maxQueryValueLength > 0 ? maxQueryValueLength : DefaultMaxQueryValueLength;

            if (allowedSchemes != null)
            {
                foreach (string scheme in allowedSchemes)
                {
                    if (!string.IsNullOrEmpty(scheme))
                    {
                        _allowedSchemes.Add(scheme);
                    }
                }
            }
        }

        /// <summary>Limits only - any scheme is accepted. Used when a service is constructed without
        /// explicit options, preserving Phase 18 behavior for every well-formed link.</summary>
        public static DeepLinkValidationOptions Default { get; } = new DeepLinkValidationOptions();

        public int MaxUriLength { get; }
        public int MaxQueryParameters { get; }
        public int MaxQueryValueLength { get; }

        /// <summary>When non-empty, a URI whose scheme is not listed (case-insensitive) is rejected -
        /// e.g. <c>{ "mygame", "https", "notification" }</c>. Empty means "any scheme". Recommended for a
        /// shipping game: list exactly the custom scheme(s)/universal-link scheme the app registers plus
        /// the scheme <c>Notifications.Integration.NotificationDeepLinkBridge</c> uses.</summary>
        public IReadOnlyCollection<string> AllowedSchemes => _allowedSchemes;

        /// <summary>Checks the raw string before it is parsed. Returns false with a log-safe reason
        /// (never echoes the URI itself).</summary>
        public bool ValidateRaw(string rawUri, out string reason)
        {
            if (rawUri.Length > MaxUriLength)
            {
                reason = $"URI exceeds {MaxUriLength} characters.";
                return false;
            }

            for (int i = 0; i < rawUri.Length; i++)
            {
                if (char.IsControl(rawUri[i]))
                {
                    reason = "URI contains control characters.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        /// <summary>Checks the parsed structure. Returns false with a log-safe reason.</summary>
        public bool ValidateParsed(DeepLink link, out string reason)
        {
            if (_allowedSchemes.Count > 0 && !_allowedSchemes.Contains(link.Scheme))
            {
                reason = $"Scheme '{link.Scheme}' is not in the allowed scheme list.";
                return false;
            }

            if (link.QueryParameters.Count > MaxQueryParameters)
            {
                reason = $"URI has more than {MaxQueryParameters} query parameters.";
                return false;
            }

            foreach (KeyValuePair<string, string> pair in link.QueryParameters)
            {
                if (pair.Key.Length > MaxQueryValueLength || (pair.Value?.Length ?? 0) > MaxQueryValueLength)
                {
                    reason = $"A query key or value exceeds {MaxQueryValueLength} characters.";
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }
}
