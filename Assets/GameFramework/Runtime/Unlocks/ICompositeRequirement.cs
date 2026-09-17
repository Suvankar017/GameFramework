using System.Collections.Generic;

namespace GameFramework.Unlocks
{
    /// <summary>Internal capability letting <see cref="UnlockService.ValidateNoCycles"/> walk into
    /// <see cref="AllRequirement"/>/<see cref="AnyRequirement"/> trees to find nested
    /// <see cref="IPrerequisiteRequirement"/> edges.</summary>
    internal interface ICompositeRequirement
    {
        IReadOnlyList<IUnlockRequirement> Children { get; }
    }
}
