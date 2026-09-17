using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Unlocks
{
    /// <summary>Logical OR — satisfied when at least one child requirement is.</summary>
    public sealed class AnyRequirement : IUnlockRequirement, ICompositeRequirement
    {
        private readonly IUnlockRequirement[] _children;

        public AnyRequirement(params IUnlockRequirement[] children)
        {
            Guard.NotNull(children, nameof(children));
            _children = children;
        }

        IReadOnlyList<IUnlockRequirement> ICompositeRequirement.Children => _children;

        public bool IsSatisfied()
        {
            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i].IsSatisfied())
                {
                    return true;
                }
            }

            return false; // vacuous OR (no children) is unsatisfied, unlike AllRequirement's vacuous AND
        }

        public string Describe() => string.Join(" OR ", System.Array.ConvertAll(_children, c => c.Describe()));
    }
}
