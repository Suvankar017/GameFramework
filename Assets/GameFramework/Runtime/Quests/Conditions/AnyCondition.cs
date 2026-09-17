using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Logical OR — satisfied when at least one child condition is.</summary>
    public sealed class AnyCondition : ICondition, ICompositeCondition
    {
        private readonly ICondition[] _children;

        public AnyCondition(params ICondition[] children)
        {
            Guard.NotNull(children, nameof(children));
            _children = children;
        }

        IReadOnlyList<ICondition> ICompositeCondition.Children => _children;

        public bool IsSatisfied()
        {
            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i].IsSatisfied())
                {
                    return true;
                }
            }

            return false; // vacuous OR (no children) is unsatisfied, unlike AllCondition's vacuous AND
        }

        public string Describe() => string.Join(" OR ", System.Array.ConvertAll(_children, c => c.Describe()));
    }
}
