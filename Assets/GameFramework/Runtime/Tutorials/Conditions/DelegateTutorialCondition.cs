using System;
using GameFramework.Core.Validation;

namespace GameFramework.Tutorials.Conditions
{
    /// <summary>The one concrete <see cref="ITutorialCondition"/> the framework ships - wraps an
    /// arbitrary game-supplied predicate, for the common case where a game's condition is a single
    /// expression (<c>() =&gt; car.Speed &gt; 10f</c>) not worth a dedicated class for. Still
    /// entirely game-agnostic: this type has no idea what the predicate checks.</summary>
    public sealed class DelegateTutorialCondition : ITutorialCondition
    {
        private readonly Func<bool> _predicate;
        private readonly string _description;

        public DelegateTutorialCondition(Func<bool> predicate, string description = null)
        {
            _predicate = Guard.NotNull(predicate, nameof(predicate));
            _description = string.IsNullOrEmpty(description) ? "Custom condition" : description;
        }

        public bool IsSatisfied() => _predicate();
        public string Describe() => _description;
    }
}
