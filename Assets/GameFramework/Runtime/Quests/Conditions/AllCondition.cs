using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Logical AND — satisfied only when every child condition is.</summary>
    public sealed class AllCondition : ICondition, ICompositeCondition
    {
        private readonly ICondition[] _children;

        public AllCondition(params ICondition[] children)
        {
            Guard.NotNull(children, nameof(children));
            _children = children;
        }

        IReadOnlyList<ICondition> ICompositeCondition.Children => _children;

        public bool IsSatisfied()
        {
            for (int i = 0; i < _children.Length; i++)
            {
                if (!_children[i].IsSatisfied())
                {
                    return false;
                }
            }

            return true;
        }

        public string Describe() => string.Join(" AND ", System.Array.ConvertAll(_children, c => c.Describe()));
    }
}
