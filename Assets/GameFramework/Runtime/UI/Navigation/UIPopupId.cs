using System;

namespace GameFramework.UI.Navigation
{
    /// <summary>Stable, designer-authored popup identifier - see <see cref="UIScreenId"/>'s remarks;
    /// same shape, separate type so a screen id can never be passed where a popup id is expected.</summary>
    public readonly struct UIPopupId : IEquatable<UIPopupId>
    {
        public static readonly UIPopupId None = default;

        public readonly string Value;

        public UIPopupId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(UIPopupId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is UIPopupId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(UIPopupId left, UIPopupId right) => left.Equals(right);
        public static bool operator !=(UIPopupId left, UIPopupId right) => !left.Equals(right);
    }
}
