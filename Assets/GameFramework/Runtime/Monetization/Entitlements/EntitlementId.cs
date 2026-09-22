using System;

namespace GameFramework.Monetization.Entitlements
{
    /// <summary>Stable, designer-authored entitlement key (e.g. "remove_ads", "premium") - mirrors
    /// <c>Rewards.RewardId</c>/<c>Progression.Economy.CurrencyId</c>'s shape and reasoning. The
    /// framework defines no built-in entitlement names - see CLAUDE.md's Phase 15 brief, section 19.</summary>
    public readonly struct EntitlementId : IEquatable<EntitlementId>
    {
        public readonly string Value;

        public EntitlementId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(EntitlementId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EntitlementId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(EntitlementId left, EntitlementId right) => left.Equals(right);
        public static bool operator !=(EntitlementId left, EntitlementId right) => !left.Equals(right);
    }
}
