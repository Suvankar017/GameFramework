using System;
using System.Collections.Generic;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// A parsed, provider-independent URI - see CLAUDE.md's Phase 18 brief, section 19. Parsing is
    /// deliberately separate from routing (section 19/20): this type carries no notion of a "route" or
    /// a matched handler, only the URI's own structure. The framework never cares whether the source
    /// was an app link, universal link, custom URL scheme, a notification, or another app (section 18)
    /// - by the time a <see cref="DeepLink"/> exists, that distinction is already gone.
    /// </summary>
    public readonly struct DeepLink
    {
        public readonly string Scheme;
        public readonly string Host;

        /// <summary>Always starts with <c>/</c> for a URI with an authority (e.g.
        /// <c>https://example.com/game/daily-reward</c> → <c>/game/daily-reward</c>); for a bare
        /// custom-scheme URI with no authority (e.g. <c>mygame://daily-reward</c>), this is the host
        /// portion re-expressed as a path segment (<c>/daily-reward</c>) so route matching does not
        /// need two different code paths for the two URI shapes.</summary>
        public readonly string Path;

        public readonly IReadOnlyDictionary<string, string> QueryParameters;
        public readonly string Fragment;
        public readonly string RawUri;

        public DeepLink(string scheme, string host, string path, IReadOnlyDictionary<string, string> queryParameters, string fragment, string rawUri)
        {
            Scheme = scheme ?? string.Empty;
            Host = host ?? string.Empty;
            Path = path ?? string.Empty;
            QueryParameters = queryParameters ?? new Dictionary<string, string>(0);
            Fragment = fragment ?? string.Empty;
            RawUri = rawUri ?? string.Empty;
        }

        public bool TryGetQueryParameter(string key, out string value) => QueryParameters.TryGetValue(key ?? string.Empty, out value);

        public override string ToString() => RawUri;
    }
}
