using GameFramework.Core.Validation;
using GameFramework.Tutorials.Steps;

namespace GameFramework.Tutorials
{
    /// <summary>
    /// One step slot in a tutorial, supplied to <see cref="ITutorialService.RegisterTutorial"/> at
    /// composition-root time - pairs display-only authoring data (<see cref="Definition"/>) with
    /// the runtime behavior that decides when the step actually completes (<see cref="Step"/>).
    /// Mirrors <see cref="Quests.Quests.QuestObjectiveEntry"/>'s shape and rationale exactly: an
    /// input/event/condition wiring cannot be authored purely as Inspector data, so it is composed
    /// in code instead of forcing a <c>[SerializeReference]</c> custom drawer.
    /// </summary>
    public readonly struct TutorialStepEntry
    {
        public readonly TutorialStepDefinition Definition;
        public readonly ITutorialStep Step;

        public TutorialStepEntry(TutorialStepDefinition definition, ITutorialStep step)
        {
            Definition = Guard.NotNull(definition, nameof(definition));
            Step = Guard.NotNull(step, nameof(step));
        }
    }
}
