using System;
using System.Security.Cryptography;
using System.Text;

namespace GameFramework.Runtime.Persistence
{
    /// <summary>
    /// SHA-256 integrity checksum for a <see cref="SaveEnvelope"/> - a standard, vetted algorithm from
    /// <c>System.Security.Cryptography</c>, never a custom hash.
    ///
    /// <para><b>What this detects:</b> accidental corruption - truncation, bit rot, a partially
    /// flushed write, a file mangled by a sync/backup tool - that still happens to parse as JSON.</para>
    ///
    /// <para><b>What this does NOT do:</b> prove the data wasn't deliberately modified. The checksum
    /// is unkeyed and computed by the client, so anyone editing a save file can recompute it. An HMAC
    /// would only help with a secret key the client doesn't hold, and a Unity client cannot hold a
    /// secret (see CLAUDE.md's Phase 19 section on client secrets). Local save data is never
    /// authoritative.</para>
    ///
    /// Runs once per save and once per load of a key - never per frame.
    /// </summary>
    internal static class SaveIntegrity
    {
        public static string Compute(int version, string payload)
        {
            byte[] input = Encoding.UTF8.GetBytes(version.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + (payload ?? string.Empty));

            byte[] hash;
            using (SHA256 sha = SHA256.Create())
            {
                hash = sha.ComputeHash(input);
            }

            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        /// <summary>True if <paramref name="envelope"/> has no checksum (written before Phase 19 -
        /// accepted for backward compatibility) or its checksum matches its contents.</summary>
        public static bool Verify(SaveEnvelope envelope)
        {
            if (string.IsNullOrEmpty(envelope.Checksum))
            {
                return true;
            }

            return string.Equals(envelope.Checksum, Compute(envelope.Version, envelope.Payload), StringComparison.OrdinalIgnoreCase);
        }
    }
}
