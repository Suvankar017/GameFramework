using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Unlocks
{
    /// <summary>Logical AND — satisfied only when every child requirement is.</summary>
    public sealed class AllRequirement : IUnlockRequirement, ICompositeRequirement
    {
        private readonly IUnlockRequirement[] _children;

        public AllRequirement(params IUnlockRequirement[] children)
        {
            Guard.NotNull(children, nameof(children));
            _children = children;
        }

        IReadOnlyList<IUnlockRequirement> ICompositeRequirement.Children => _children;

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
