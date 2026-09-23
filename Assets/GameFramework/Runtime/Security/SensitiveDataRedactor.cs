using System;
using System.Text.RegularExpressions;

namespace GameFramework.Runtime.Security
{
    /// <summary>
    /// Explicit, boundary-only redaction for text that may carry credentials or personal data -
    /// e.g. a deep-link URI about to be logged, a diagnostic context value, or an error message about
    /// to be forwarded to a crash-reporting provider.
    ///
    /// <para>Deliberately <b>not</b> wired into <c>LoggingService.Log</c> itself: running a regex over
    /// every log message would tax every call site in the framework for the handful of places that
    /// can actually see untrusted/sensitive text. Call it at those trust boundaries instead (see
    /// CLAUDE.md's Phase 19 section, "Logging redaction").</para>
    ///
    /// <para>Redaction is heuristic, key-name based: it replaces the <i>value</i> of any
    /// <c>key=value</c>/<c>key: value</c>/<c>"key":"value"</c> pair whose key contains a known
    /// sensitive fragment (token, secret, password, api key, authorization, receipt, ...), plus the
    /// credential following <c>Bearer</c>/<c>Basic</c>. It cannot recognize a secret with no
    /// identifying key - the reliable fix for that is never putting secrets in a client build or a
    /// log line in the first place.</para>
    /// </summary>
    public static class SensitiveDataRedactor
    {
        /// <summary>The replacement text for a redacted value.</summary>
        public const string Mask = "***";

        // Lower-case fragments matched against a lower-cased key. Kept short and specific: over-matching
        // only costs readability of a diagnostic line, under-matching leaks a credential.
        private static readonly string[] SensitiveKeyFragments =
        {
            "token", "secret", "password", "passwd", "apikey", "api_key", "api-key",
            "authorization", "credential", "receipt", "signature", "cookie", "session_id",
            "sessionid", "private_key", "privatekey", "client_secret", "email", "phone"
        };

        // Group 1 = key, group 2 = separator (with optional opening quote), group 3 = value. The value
        // stops at whitespace or a structural delimiter (&, quote, comma, semicolon, brackets) so a query
        // string or JSON object is redacted pair-by-pair rather than swallowed whole.
        private static readonly Regex KeyValuePattern = new Regex(
            "([A-Za-z0-9_\\-\\.]*(?:token|secret|password|passwd|api[_\\-]?key|authorization|credential|receipt|signature|cookie|session_?id|private[_\\-]?key|email|phone)[A-Za-z0-9_\\-\\.]*)(\"?\\s*[=:]\\s*\"?)([^\\s&\"',;\\}\\]]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex AuthorizationSchemePattern = new Regex(
            "\\b(Bearer|Basic)\\s+[A-Za-z0-9\\-\\._~\\+/=]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>True if <paramref name="key"/> names a value that must never be logged or
        /// forwarded verbatim (case-insensitive fragment match).</summary>
        public static bool IsSensitiveKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            for (int i = 0; i < SensitiveKeyFragments.Length; i++)
            {
                if (key.IndexOf(SensitiveKeyFragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns <see cref="Mask"/> if <paramref name="key"/> is sensitive, otherwise
        /// <paramref name="value"/> unchanged - for structured key/value data (diagnostic context,
        /// tags, parameters) where the key is already known.</summary>
        public static string RedactValue(string key, string value) =>
            IsSensitiveKey(key) && !string.IsNullOrEmpty(value) ? Mask : value;

        /// <summary>
        /// Redacts sensitive <c>key=value</c> pairs and <c>Bearer</c>/<c>Basic</c> credentials inside
        /// free-form text (a URI, an error message). Returns the input unchanged (same instance, no
        /// allocation) when it contains no <c>=</c>/<c>:</c>/auth scheme at all, which is the common
        /// case - so calling this at a logging boundary costs one character scan for ordinary text.
        /// </summary>
        public static string Redact(string text)
        {
            if (string.IsNullOrEmpty(text) || !MightContainSecret(text))
            {
                return text;
            }

            string result = KeyValuePattern.Replace(text, match => match.Groups[1].Value + match.Groups[2].Value + Mask);
            return AuthorizationSchemePattern.Replace(result, match => match.Groups[1].Value + " " + Mask);
        }

        /// <summary>
        /// A log-safe rendering of a URI: <see cref="Redact"/> applied, then truncated to
        /// <paramref name="maxLength"/> characters (an oversized URI is itself a rejection reason, and
        /// echoing all of it back into the log would just move the problem).
        /// </summary>
        public static string RedactUri(string uri, int maxLength = 256)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return uri ?? string.Empty;
            }

            string redacted = Redact(uri);
            return redacted.Length <= maxLength ? redacted : redacted.Substring(0, Math.Max(0, maxLength)) + "...(truncated)";
        }

        private static bool MightContainSecret(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '=' || c == ':')
                {
                    return true;
                }
            }

            return text.IndexOf("bearer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("basic", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
