using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Logical NOT — satisfied exactly when its inner condition is not.</summary>
    public sealed class NotCondition : ICondition, ICompositeCondition
    {
        private readonly ICondition _inner;

        public NotCondition(ICondition inner)
        {
            _inner = Guard.NotNull(inner, nameof(inner));
        }

        IReadOnlyList<ICondition> ICompositeCondition.Children => new[] { _inner };

        public bool IsSatisfied() => !_inner.IsSatisfied();

        public string Describe() => $"NOT ({_inner.Describe()})";
    }
}
