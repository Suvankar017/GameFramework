using System;

namespace GameFramework.Runtime.Persistence
{
    /// <summary>
    /// The on-disk wrapper around every saved payload: a version number plus the payload as its
    /// own already-serialized JSON text (double-encoded), rather than a nested JSON object. That
    /// trade-off (slightly less readable raw output, one extra escape/unescape pass) is what lets
    /// <see cref="PersistenceService"/> read <see cref="Version"/> and hand <see cref="Payload"/>
    /// to a migration without needing to know the payload's shape up front — JsonUtility has no
    /// generic "parse into an untyped tree" mode, so a nested-object envelope would force knowing
    /// the current <c>TData</c> type just to read the version.
    ///
    /// <see cref="Checksum"/> (added in Phase 19) is a SHA-256 of <see cref="Version"/> and
    /// <see cref="Payload"/> - see <see cref="SaveIntegrity"/>. It is additive and optional on read:
    /// an envelope written before Phase 19 has no checksum and is still accepted (and gains one on its
    /// next save); an older build reading a newer envelope simply ignores the extra field, since
    /// JsonUtility skips unknown fields.
    /// </summary>
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int Version;
        public string Payload;
        public string Checksum;
    }
}
