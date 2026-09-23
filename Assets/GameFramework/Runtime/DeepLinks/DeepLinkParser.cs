using System;
using System.Collections.Generic;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// Stateless URI → <see cref="DeepLink"/> parsing, built entirely on <see cref="Uri"/> (no custom
    /// URI grammar) - see CLAUDE.md's Phase 18 brief, section 19. Never throws; a malformed URI simply
    /// returns false (section 36: all incoming data is untrusted).
    /// </summary>
    public static class DeepLinkParser
    {
        public static bool TryParse(string rawUri, out DeepLink link)
        {
            link = default;

            if (string.IsNullOrWhiteSpace(rawUri) || !Uri.TryCreate(rawUri, UriKind.Absolute, out Uri uri) || string.IsNullOrEmpty(uri.Scheme))
            {
                return false;
            }

            bool isWebScheme = string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);

            string path;
            if (isWebScheme)
            {
                path = uri.AbsolutePath;
            }
            else
            {
                // Custom scheme (e.g. "mygame://daily-reward?source=notification"): .NET's generic
                // URI parser treats "daily-reward" as the Host/authority component (the "//" signals
                // one), even though it functions as the first path segment here. Fold Host + Path into
                // one normalized Path so route matching never needs two different code paths depending
                // on which URI shape delivered the link (section 19: parsing hides this from routing).
                string combined = (uri.Host ?? string.Empty) + uri.AbsolutePath;
                combined = combined.Trim('/');
                path = combined.Length > 0 ? "/" + combined : string.Empty;
            }

            var query = new Dictionary<string, string>(StringComparer.Ordinal);
            string rawQuery = uri.Query;
            if (!string.IsNullOrEmpty(rawQuery))
            {
                foreach (string pair in rawQuery.TrimStart('?').Split('&'))
                {
                    if (string.IsNullOrEmpty(pair))
                    {
                        continue;
                    }

                    int separatorIndex = pair.IndexOf('=');
                    string key = separatorIndex >= 0 ? pair.Substring(0, separatorIndex) : pair;
                    string value = separatorIndex >= 0 ? pair.Substring(separatorIndex + 1) : string.Empty;

                    key = SafeUnescape(key);
                    value = SafeUnescape(value);

                    if (!string.IsNullOrEmpty(key))
                    {
                        query[key] = value;
                    }
                }
            }

            string fragment = uri.Fragment.TrimStart('#');

            link = new DeepLink(uri.Scheme, uri.Host, path, query, fragment, rawUri);
            return true;
        }

        private static string SafeUnescape(string value)
        {
            try
            {
                return Uri.UnescapeDataString(value);
            }
            catch (UriFormatException)
            {
                return value;
            }
        }
    }
}
