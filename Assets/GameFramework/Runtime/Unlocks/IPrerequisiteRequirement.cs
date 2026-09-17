namespace GameFramework.Unlocks
{
    /// <summary>Internal capability letting <see cref="UnlockService.ValidateNoCycles"/> discover
    /// prerequisite edges in a registered requirement tree without a type-check per concrete
    /// requirement class.</summary>
    internal interface IPrerequisiteRequirement
    {
        UnlockId TargetId { get; }
    }
}
