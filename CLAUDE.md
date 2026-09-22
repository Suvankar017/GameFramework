# CLAUDE.md

This file defines the engineering rules, workflow, architecture expectations, and safety constraints for working on this Unity project.

Claude must read and follow this file before making any code, asset, scene, prefab, configuration, or project-setting changes.

---

# 1. Project Overview

This is a production Unity project.

## Primary Goals

* Write maintainable, production-ready code.
* Prefer simple and robust solutions over unnecessary abstraction.
* Preserve existing architecture unless there is a strong technical reason to change it.
* Avoid introducing dependencies without explicit justification.
* Optimize for mobile where applicable.
* Minimize runtime allocations and unnecessary CPU/GPU work.
* Make changes incrementally and safely.
* Keep systems testable and extensible.
* Avoid breaking existing functionality while implementing new features.

---

# 2. Unity Environment

Target Unity version:

* Unity `2022.3.62f3`

Language:

* C#
* C# 9.0 compatible syntax

Do NOT assume APIs introduced after Unity 2022.3 are available.

Before using an API:

1. Verify it exists in the target Unity version.
2. Check package compatibility if it belongs to a Unity package.
3. Prefer APIs already used by the project.

## Important Compatibility Rule

Do not blindly use modern .NET APIs that are unavailable in Unity 2022.3.

For example:

* Do not assume `PriorityQueue<TElement,TPriority>` exists.
* Do not assume APIs from newer Unity versions.
* Do not use newer C# language features unless confirmed compatible.

If an equivalent implementation is required, create a Unity-compatible solution.

---

# 3. General Engineering Philosophy

Follow these principles:

1. Correctness first.
2. Simplicity second.
3. Maintainability third.
4. Performance where it matters.
5. Extensibility without speculative abstraction.

Do not over-engineer simple features.

Do not create:

* unnecessary interfaces
* unnecessary factories
* unnecessary service layers
* unnecessary generic abstractions
* unnecessary event buses
* unnecessary managers
* unnecessary ScriptableObject layers

An abstraction must have a practical reason.

---

# 4. Existing Architecture Comes First

Before modifying a system:

1. Inspect the existing implementation.
2. Identify how the system currently works.
3. Find its consumers.
4. Find related data/configuration.
5. Check prefabs/scenes that depend on it.
6. Check assembly definitions.
7. Check existing utilities that solve part of the problem.

Do NOT immediately rewrite an existing system.

If the current architecture is reasonable, extend it.

If the architecture is problematic, explain:

* what is wrong
* why it matters
* what the smallest safe improvement is

Do not perform a large architectural migration unless explicitly requested.

---

# 5. Required Workflow

For every non-trivial task, follow this workflow.

## Step 1 — Understand

Inspect:

* relevant scripts
* related systems
* prefabs
* scenes
* ScriptableObjects
* assembly definitions
* package dependencies
* existing utilities

Do not make assumptions when the project can be inspected.

## Step 2 — Plan

Before implementing a substantial feature, determine:

* affected files
* affected systems
* dependencies
* data flow
* runtime flow
* editor requirements
* serialization requirements
* performance implications
* backward compatibility concerns

## Step 3 — Implement

Make the smallest coherent change that solves the problem.

## Step 4 — Validate

Check:

* compile errors
* namespace errors
* assembly references
* missing serialized fields
* null-reference risks
* lifecycle issues
* editor/runtime separation
* performance problems

## Step 5 — Review

Before finishing, review the implementation for:

* unnecessary allocations
* duplicated logic
* incorrect Unity lifecycle usage
* hidden dependencies
* race conditions
* serialization problems
* scene/prefab breakage
* backwards compatibility

---

# 6. File and Folder Organization

Follow the existing project structure.

Do not move existing files simply for aesthetic reasons.

For new reusable systems, prefer a structure similar to:

```text
Assets/
└── Game/
    └── <SystemName>/
        ├── Runtime/
        ├── Editor/
        ├── Data/
        ├── Components/
        ├── Services/
        ├── Utilities/
        ├── Tests/
        └── <SystemName>.asmdef
```

If the project already has a package-specific structure, follow that instead.

Runtime code must not depend on Editor-only assemblies.

Editor tools must live in an Editor assembly/folder.

Tests must be isolated from production runtime assemblies.

---

# 7. Assembly Definitions

Use assembly definitions for meaningful reusable systems.

Rules:

* Runtime assemblies must not reference Editor assemblies.
* Editor assemblies may reference runtime assemblies.
* Test assemblies should reference only what they need.
* Avoid circular assembly dependencies.
* Do not modify existing `.asmdef` dependencies unless necessary.

When adding an assembly reference, verify the dependency direction.

Preferred dependency direction:

```text
Core
 ↑
Runtime Systems
 ↑
Features
 ↑
Game/UI
```

Avoid:

```text
Core ↔ Feature
```

or:

```text
Runtime → Editor
```

---

# 8. Namespace Rules

Use block-style namespaces.

Preferred:

```csharp
namespace Game.Systems
{
    public class Example
    {
    }
}
```

Do NOT use file-scoped namespaces:

```csharp
namespace Game.Systems;
```

Namespaces should reflect the system/module.

Avoid meaningless namespaces such as:

```text
Utilities
Helpers
Misc
Common
Stuff
```

unless the existing project already uses them.

---

# 9. C# Coding Standards

## Classes

Use clear PascalCase names.

```csharp
public class LevelProgressService
{
}
```

Private fields:

```csharp
private int _currentLevel;
```

Serialized fields:

```csharp
[SerializeField]
private float _moveSpeed = 5f;
```

Constants:

```csharp
private const int MaxLevels = 200;
```

Methods:

```csharp
public void UnlockNextLevel()
{
}
```

---

# 10. Naming

Use:

* PascalCase for classes
* PascalCase for methods
* PascalCase for public properties
* camelCase for parameters
* `_camelCase` for private fields
* PascalCase for constants when appropriate

Avoid abbreviations unless universally understood.

Prefer:

```csharp
maximumHealth
```

over:

```csharp
maxHp
```

unless the existing codebase consistently uses the abbreviation.

---

# 11. Null Handling

Unity objects require special consideration because destroyed UnityEngine.Object instances can compare equal to null.

Use appropriate Unity null checks.

Avoid excessive defensive checks that hide programming errors.

For required dependencies:

```csharp
if (_controller == null)
{
    Debug.LogError(...);
    return;
}
```

For required serialized references, prefer validation in:

* `Awake`
* `OnValidate`
* custom editor tooling

when appropriate.

---

# 12. MonoBehaviour Lifecycle

Use Unity lifecycle methods deliberately.

Preferred general order:

```text
Awake
OnEnable
Start
Update
LateUpdate
OnDisable
OnDestroy
```

Do not put expensive initialization into `Update`.

Do not repeatedly perform component lookups in `Update`.

Avoid:

```csharp
GetComponent<T>()
```

inside frequently executed loops when the reference can be cached.

Avoid:

```csharp
FindObjectOfType<T>()
FindObjectsOfType<T>()
GameObject.Find()
```

during gameplay unless there is a specific justified reason.

---

# 13. Update Loops

Do not add an `Update()` method just because it is convenient.

Before adding an update loop, ask:

* Can this be event driven?
* Can it be updated only while active?
* Can it use a coroutine?
* Can it use a timer?
* Can it update at a lower frequency?
* Can multiple updates be batched?

If an update loop is necessary, keep it cheap.

---

# 14. Garbage Collection

This project targets mobile.

Avoid allocations in hot paths.

Be particularly careful with:

* LINQ
* closures
* lambdas inside loops
* string concatenation
* repeated string interpolation
* temporary arrays
* boxing
* enumerators
* frequent collection creation
* `Instantiate`/`Destroy` churn
* repeated `GetComponents`
* reflection

Do not blindly optimize everything.

Optimize code that runs:

* every frame
* many times per frame
* for many objects
* during gameplay
* during physics simulation

---

# 15. Collections

Avoid repeatedly allocating collections.

Prefer:

```csharp
private readonly List<T> _items = new List<T>();
```

and reuse them when appropriate.

When performance matters:

* pre-size collections
* reuse buffers
* avoid unnecessary conversions
* avoid creating temporary lists every frame

Do not introduce custom pooling unless pooling actually provides value.

---

# 16. Unity Serialization

Serialized fields are part of the project's data contract.

Changing:

```csharp
[SerializeField]
private int _value;
```

to another type can break existing scene/prefab data.

Before modifying serialized data:

* search for prefab usage
* search for scene usage
* check existing serialized values
* consider migration/backwards compatibility

Use:

```csharp
FormerlySerializedAs
```

when renaming serialized fields and appropriate.

Example:

```csharp
[FormerlySerializedAs("_oldValue")]
[SerializeField]
private int _newValue;
```

Do not casually remove serialized fields.

---

# 17. ScriptableObjects

Use ScriptableObjects for shared configuration/data where appropriate.

Good use cases:

* game configuration
* level configuration
* balancing data
* item definitions
* localization data
* reusable settings

Avoid using ScriptableObjects merely as global mutable state.

Prefer immutable/read-only configuration during runtime when possible.

---

# 18. Singleton and Global State

Do not introduce a singleton automatically.

Before creating one, determine whether:

* an existing singleton already provides the service
* dependency injection is practical
* a scene-level reference is sufficient
* a service locator already exists

If the project already uses a service locator, integrate with it rather than creating a competing global architecture.

Do not introduce:

```text
GameManager2
AudioManager2
UIManager2
NewServiceLocator
```

to solve problems already handled by existing systems.

---

# 19. Dependency Injection

Use dependency injection where it improves testability or removes hard dependencies.

Do not create a DI framework for a small feature.

Prefer simple dependency injection:

```csharp
public void Initialize(ILevelProgressService progressService)
{
    _progressService = progressService;
}
```

or constructor injection for pure C# classes.

Unity `MonoBehaviour` dependencies may use:

* serialized references
* initialization methods
* composition roots
* existing project service infrastructure

---

# 20. Interfaces

Create interfaces when there is a meaningful abstraction.

Good:

```csharp
public interface ILevelProgressService
{
    bool IsUnlocked(int level);
    bool IsCompleted(int level);
}
```

Bad:

```csharp
public interface IExample
{
}
```

Do not create interfaces solely to follow SOLID mechanically.

---

# 21. SOLID

Apply SOLID pragmatically.

## Single Responsibility

A class should have a clear responsibility.

Avoid giant classes that handle:

* data
* UI
* persistence
* gameplay
* networking
* analytics
* audio

all together.

## Open/Closed

Design extension points where future variation is reasonably expected.

## Liskov

Derived classes must preserve the expected behavior of the base class.

## Interface Segregation

Prefer small focused interfaces.

## Dependency Inversion

Core logic should not unnecessarily depend on concrete infrastructure.

Do not over-apply SOLID to simple Unity components.

---

# 22. Error Handling

Never silently swallow errors.

Bad:

```csharp
try
{
    ...
}
catch
{
}
```

If an exception is intentionally handled, explain why.

Use appropriate logging.

Differentiate:

* developer/programming errors
* recoverable runtime errors
* expected conditions
* user-facing failures

Do not spam the console every frame.

---

# 23. Logging

Use the project's logging framework if one exists.

Do not introduce a second logging system.

If the project provides custom logging utilities, use them consistently.

Logs should contain enough context to diagnose the issue.

Prefer:

```text
[LevelProgress] Failed to load progress for profile X.
```

over:

```text
error
```

Avoid logging inside high-frequency loops unless debugging is explicitly enabled.

---

# 24. Debug Code

Do not leave temporary debugging code in production code.

Avoid committing:

```csharp
Debug.Log("TEST");
```

or:

```csharp
Debug.Log("HERE");
```

Remove temporary:

* logs
* gizmos
* test objects
* debug UI
* experimental code
* commented-out implementations

before completing the task.

---

# 25. Editor Tools

When creating Unity Editor tools:

* Prefer UI Toolkit for new editor UI.
* Keep Editor-only code isolated.
* Do not introduce runtime dependencies for editor functionality.
* Provide useful validation and error messages.
* Avoid destructive operations without confirmation.
* Support Undo where modifying project data.

Use:

```csharp
Undo.RecordObject(...)
```

when appropriate.

For asset creation/modification, mark assets dirty correctly.

---

# 26. Undo / Redo

Editor tools that modify scene objects or serialized assets should support Undo whenever practical.

Use Unity's Undo API.

Do not directly mutate serialized project data without considering:

* Undo
* dirty state
* prefab overrides
* serialization

---

# 27. Prefabs and Scenes

Before modifying a prefab or scene:

1. Determine whether it is used by multiple systems.
2. Determine whether the change affects serialized references.
3. Preserve existing references.
4. Avoid unnecessary hierarchy changes.

Do not randomly rename:

* GameObjects
* components
* serialized fields
* assets

because other systems may depend on them.

---

# 28. Prefab Safety

When changing prefab structure, consider:

* prefab overrides
* nested prefabs
* scene instances
* serialized references
* scripts referencing transforms/components by hierarchy

If a change can break existing prefab instances, call it out before making a destructive change.

---

# 29. Performance

The project targets mobile hardware.

Always consider:

## CPU

* Update frequency
* physics cost
* allocations
* component count
* expensive searches
* excessive callbacks
* unnecessary serialization work

## GPU

* overdraw
* transparency
* shader complexity
* texture size
* draw calls
* batching
* UI complexity

## Memory

* texture memory
* mesh memory
* instantiated objects
* duplicated assets
* temporary allocations

Do not optimize based purely on assumptions.

If profiling data exists, use it.

---

# 30. Mobile Optimization

Prefer:

* object pooling where justified
* cached references
* event-driven logic
* batched processing
* low allocation code
* appropriate texture sizes
* minimal transparent overdraw
* efficient UI
* controlled physics simulation

Avoid premature micro-optimizations.

Do not sacrifice maintainability for negligible performance gains.

---

# 31. Physics

Use the correct Unity physics lifecycle.

For Rigidbody physics:

* physics state changes generally belong in `FixedUpdate`
* visual follow-up may belong in `LateUpdate`

Avoid changing physics state from arbitrary update loops unless there is a clear reason.

Do not repeatedly create/destroy physics objects during gameplay without considering pooling.

---

# 32. Coroutines

Use coroutines for:

* timed sequences
* asynchronous Unity workflows
* waiting for conditions
* staged initialization

Do not use coroutines as a replacement for ordinary synchronous logic.

Avoid starting duplicate coroutines accidentally.

If a coroutine must be stopped, maintain a clear lifecycle.

---

# 33. Async Code

Do not introduce async/await unless it is appropriate for the task.

When using asynchronous operations:

* handle cancellation where appropriate
* handle exceptions
* avoid updating destroyed Unity objects
* consider scene changes
* consider object lifetime

Do not assume Unity's synchronization behavior without verifying it.

---

# 34. Threading

UnityEngine APIs are generally main-thread only.

Do not access Unity objects from background threads.

Pure data processing may be moved off-thread only when:

* thread safety is understood
* data ownership is clear
* synchronization is correct

Do not introduce multithreading merely for perceived performance.

---

# 35. Addressables / Asset Loading

If the project already uses Addressables, follow the existing architecture.

Do not mix:

* Resources
* Addressables
* direct asset references

without a clear reason.

Avoid `Resources.Load` for large scalable systems unless explicitly intended.

Always consider asset lifetime and release behavior.

---

# 36. Object Pooling

Use pooling when objects are repeatedly created and destroyed during gameplay.

Typical candidates:

* projectiles
* particles
* temporary effects
* repeated UI elements
* frequently spawned gameplay objects

Do not pool everything.

Pooling introduces lifecycle complexity, so only use it where it solves a real problem.

---

# 37. UI

Follow the existing UI architecture.

For new UI:

* use UGUI if the existing feature uses UGUI
* use UI Toolkit for new Editor tooling unless there is a strong reason otherwise
* preserve existing Canvas architecture
* avoid unnecessary layout rebuilds
* avoid per-frame UI updates
* cache component references

For dynamically created UI:

* prefer pooling where appropriate
* avoid repeatedly rebuilding large hierarchies
* avoid unnecessary `LayoutRebuilder` calls

---

# 38. Localization

Never hard-code user-facing text when the project has a localization system.

Before adding UI text:

1. Find the existing localization system.
2. Add a localization key.
3. Add required translations according to the project's supported languages.
4. Use the existing localization API.

Do not create a second localization system.

---

# 39. Save Data

Treat save data as a compatibility contract.

Before changing save structures:

* inspect the existing save format
* consider older versions
* preserve backward compatibility where practical
* add migration/versioning when necessary

Never silently reset user progress.

If a migration is needed, make it explicit.

---

# 40. Gameplay Progression

When implementing progression systems:

* distinguish unlocked from completed
* distinguish first completion from replay
* persist state
* handle invalid level indices
* avoid relying solely on scene state
* make reward logic deterministic

Example conceptual state:

```text
Locked
Unlocked
Completed
```

Do not infer completion merely from the currently selected level.

---

# 41. Data Validation

Validate data at the appropriate boundary.

Examples:

* level index range
* null references
* invalid configuration
* duplicate IDs
* invalid serialized values
* missing assets

Editor validation should catch problems before runtime when practical.

Runtime validation should protect against corrupted or unexpected data.

---

# 42. IDs

IDs must be stable and unique within their intended scope.

Do not generate IDs using:

```csharp
GetInstanceID()
```

if the ID needs to persist between sessions.

Do not use object names as persistent IDs.

For persistent IDs, use an explicit serialized/generated identifier.

---

# 43. Randomness

If randomness affects:

* gameplay
* save data
* progression
* simulation
* tests

consider whether deterministic random seeds are required.

Do not use uncontrolled randomness when deterministic reproduction is important.

---

# 44. Testing

For non-trivial systems, add tests where practical.

Prefer testing pure logic independently from Unity scene state.

Test:

* normal cases
* boundary cases
* invalid input
* repeated calls
* persistence behavior
* first-time vs repeat behavior
* state transitions

Do not create tests that merely test Unity itself.

---

# 45. Regression Testing

When fixing a bug:

1. Understand the original failure.
2. Identify the root cause.
3. Fix the root cause.
4. Add a regression test when practical.
5. Check related behavior.

Do not apply a superficial workaround if the underlying cause is clear.

---

# 46. Package Dependencies

Do not add third-party packages without checking:

* Unity compatibility
* package version
* license
* mobile performance
* build impact
* dependency conflicts
* maintenance status

Prefer existing project dependencies.

If a new package is genuinely necessary, explain why.

---

# 47. Git Safety

Git is the source of truth.

Do not:

* delete unrelated changes
* revert user modifications
* reset the repository
* force checkout
* force push
* rewrite history

unless explicitly requested.

Before modifying a file, consider whether it contains uncommitted user work.

Never overwrite unrelated changes simply to make the task easier.

---

# 48. Never Destroy User Work

This is a strict rule.

Do not:

```text
git reset --hard
git clean -fd
git checkout -- .
```

or equivalent destructive operations unless the user explicitly requests them.

Do not replace an existing implementation wholesale when a targeted change is sufficient.

---

# 49. Working Tree Awareness

Before making broad changes:

* inspect Git status
* identify existing modifications
* avoid modifying unrelated files

If a file already contains user changes, preserve them.

Do not assume uncommitted code is broken or disposable.

---

# 50. Claude Code / MCP Usage

When Unity MCP or Unity CLI tools are available, prefer them for Unity-specific inspection and operations.

Use Unity tooling to inspect:

* scenes
* GameObjects
* components
* prefabs
* assets
* project settings
* compilation state
* Unity console errors

Do not fabricate tool output.

If a tool operation fails:

1. inspect the error
2. determine the cause
3. retry only when appropriate
4. do not blindly repeat destructive operations

---

# 51. Unity CLI

When invoking Unity through CLI:

* use the project's configured Unity version
* use batch mode carefully
* preserve the project state
* capture exit codes
* inspect logs when compilation/import fails

A successful Unity process exit code does not necessarily mean the feature is correct.

Always inspect relevant errors/warnings when validating code.

---

# 52. Compilation Validation

After modifying C# code:

1. Allow Unity to compile.
2. Check compiler errors.
3. Fix all errors caused by the change.
4. Check warnings introduced by the change.
5. Re-test relevant functionality.

Do not declare a task complete while known compilation errors remain.

---

# 53. Avoid Fake Validation

Never say:

* "tested successfully"
* "compiles correctly"
* "works in Unity"

unless it was actually verified.

If execution/testing was not possible, state that clearly.

Distinguish between:

```text
Implemented
```

and:

```text
Implemented and verified
```

---

# 54. Search Before Creating

Before creating a new class, method, utility, or system:

Search the project for existing functionality.

For example, before creating:

```text
Timer
EventBus
Singleton
SaveManager
ObjectPool
Logger
UIManager
LocalizationManager
ServiceLocator
```

check whether an existing implementation already exists.

Reuse it when appropriate.

---

# 55. Avoid Duplicate Systems

Never create parallel systems without a strong reason.

Bad:

```text
Existing AudioManager
New AudioService
AnotherAudioManager
```

Bad:

```text
Existing LocalizationManager
New LocalizationService
```

Prefer extending or adapting the existing system.

---

# 56. API Design

Public APIs are contracts.

Before changing a public method/property:

* find all callers
* check serialized UnityEvent references
* check reflection usage
* check editor tooling
* check tests
* preserve compatibility when possible

Prefer additive changes over breaking changes.

---

# 57. Backward Compatibility

When changing an existing API:

Prefer:

```csharp
public void NewMethod(...)
{
}

[Obsolete("Use NewMethod instead.")]
public void OldMethod(...)
{
    NewMethod(...);
}
```

when maintaining compatibility is appropriate.

Do not remove old APIs casually.

---

# 58. Comments

Write comments explaining:

* why something is necessary
* non-obvious constraints
* performance decisions
* Unity-specific behavior
* serialization compatibility
* algorithmic reasoning

Do not write comments that merely restate the code.

Bad:

```csharp
// Increment counter
_counter++;
```

Good:

```csharp
// Keep the simulation deterministic by advancing this counter
// only from the fixed simulation step.
_counter++;
```

---

# 59. TODOs

Do not leave vague TODOs.

Bad:

```csharp
// TODO: fix this later
```

Good:

```csharp
// TODO(HairSimulation): Replace temporary allocation with reusable buffer
// after profiling confirms this path is allocation-heavy.
```

Only add TODOs when the future work is meaningful.

---

# 60. Security and Secrets

Never hard-code:

* API keys
* passwords
* private tokens
* credentials
* signing secrets

Do not expose secrets in:

* source code
* logs
* commits
* configuration checked into Git

If credentials are encountered, do not copy them into generated files or responses.

---

# 61. Asset Changes

Be conservative when modifying assets.

Do not automatically regenerate or overwrite:

* textures
* materials
* prefabs
* scenes
* animations
* ScriptableObjects

unless the task requires it.

Preserve import settings unless changing them is part of the task.

---

# 62. Scene Changes

Scene files are highly sensitive.

Before modifying a scene:

* identify relevant objects
* preserve unrelated objects
* preserve component values
* preserve hierarchy unless necessary
* avoid unnecessary scene-wide changes

Do not save a scene merely because it was opened unless changes were intentionally made.

---

# 63. Prefab Changes

Treat prefab changes as potentially global changes.

A prefab modification may affect multiple scenes.

Before changing one:

* find usages
* understand whether the change is intended globally
* preserve overrides where possible

---

# 64. Generated Code

Generated code must:

* compile
* follow project conventions
* have clear ownership
* avoid being edited manually if regeneration is expected

If a generator is modified, verify generated output.

Do not manually patch generated files unless explicitly intended.

---

# 65. API Documentation

For reusable public systems, document important public APIs.

Documentation should explain:

* purpose
* lifecycle
* expected usage
* important constraints
* thread/lifecycle requirements

Do not document every trivial property.

---

# 66. Production Readiness Checklist

Before declaring a feature complete, verify:

## Code

* [ ] Code compiles.
* [ ] Names are clear.
* [ ] No unnecessary duplication.
* [ ] No temporary debug code.
* [ ] No obvious null-reference risks.
* [ ] No unnecessary allocations in hot paths.
* [ ] Existing architecture is respected.

## Unity

* [ ] Serialized fields are safe.
* [ ] Prefabs are not accidentally broken.
* [ ] Scenes are not accidentally modified.
* [ ] Lifecycle methods are appropriate.
* [ ] Editor/runtime boundaries are correct.

## Performance

* [ ] No unnecessary per-frame work.
* [ ] No repeated expensive lookups.
* [ ] No obvious allocation spikes.
* [ ] Mobile performance implications considered.

## Persistence

* [ ] Save compatibility considered.
* [ ] Invalid data handled.
* [ ] State transitions are deterministic.

## Testing

* [ ] Relevant behavior tested.
* [ ] Edge cases considered.
* [ ] Regression risk considered.

## Git

* [ ] No unrelated files changed.
* [ ] Existing user changes preserved.
* [ ] No destructive Git operations performed.

---

# 67. When Requirements Are Ambiguous

Do not invent important requirements.

If ambiguity materially affects implementation:

1. Identify the ambiguity.
2. Inspect the existing project for clues.
3. If the answer can be determined from the codebase, determine it yourself.
4. Otherwise ask a concise clarification.

Do not ask unnecessary questions when a reasonable implementation is obvious.

---

# 68. When Multiple Solutions Exist

Prefer the solution that:

1. Fits the existing architecture.
2. Has the smallest surface area.
3. Is easy to understand.
4. Is easy to test.
5. Has good runtime performance.
6. Is easy to extend later.

Do not choose a sophisticated architecture simply because it is theoretically more scalable.

---

# 69. Implementation Size

Do not artificially split a small feature into many files.

For example, a simple feature may reasonably contain:

```text
Feature.cs
FeatureData.cs
FeatureEditor.cs
```

rather than:

```text
IFeature.cs
Feature.cs
IFeatureService.cs
FeatureService.cs
FeatureFactory.cs
FeatureProvider.cs
FeatureController.cs
FeatureRepository.cs
FeatureRegistry.cs
```

unless those abstractions have real value.

---

# 70. Refactoring Rule

Do not combine feature implementation with unrelated refactoring.

If the task is:

```text
Fix level unlock bug
```

do not also:

* rename the entire progression system
* restructure folders
* replace the save architecture
* introduce a new DI framework
* rewrite UI

unless explicitly requested.

Keep changes focused.

---

# 71. Bug-Fixing Rule

For bugs, prefer:

```text
Observe → Reproduce → Isolate → Understand → Fix → Validate
```

Do not immediately rewrite the affected system.

When possible, identify the exact root cause before changing code.

---

# 72. Performance Bug Rule

For performance problems:

```text
Measure → Identify bottleneck → Optimize → Measure again
```

Do not assume the bottleneck.

Do not optimize code that is not contributing meaningfully to the problem.

---

# 73. Architecture Change Rule

If a requested feature cannot reasonably fit into the existing architecture:

1. Explain the conflict.
2. Identify the minimum architectural change.
3. Preserve existing APIs where possible.
4. Migrate incrementally.
5. Do not leave the project in a half-migrated state.

---

# 74. Final Response Format

After completing a task, provide a concise summary.

Preferred format:

```text
## Implemented

- <change>
- <change>
- <change>

## Validation

- <what was actually tested>
- <what was verified>

## Files Changed

- <file>
- <file>

## Notes

- <important caveat, if any>
```

Do not claim validation that was not performed.

If there are remaining issues, explicitly list them.

---

# 75. Absolute Rules

These rules override convenience.

1. Do not destroy user work.
2. Do not use destructive Git commands without explicit permission.
3. Do not silently change unrelated systems.
4. Do not invent APIs or project architecture.
5. Do not assume a newer Unity/.NET API is available.
6. Do not leave compilation errors.
7. Do not hide errors.
8. Do not introduce duplicate systems unnecessarily.
9. Do not hard-code secrets.
10. Do not claim tests were performed when they were not.
11. Preserve serialized Unity data.
12. Prefer simple, production-ready implementations.
13. Inspect before modifying.
14. Validate after modifying.
15. Keep changes focused and reversible.

---

# 76. Claude Operating Principle

Act as a senior Unity engineer working inside an existing production codebase.

Do not behave like a code generator that blindly creates files.

Your job is to:

```text
Understand the project
        ↓
Understand the existing architecture
        ↓
Identify the smallest correct solution
        ↓
Implement it cleanly
        ↓
Validate it
        ↓
Protect existing functionality
```

The goal is not to produce the most code.

The goal is to produce the **smallest maintainable production-quality change that correctly solves the problem.**

---

# 77. Phase 5 Performance Infrastructure

Phase 5 added `GameFramework.Performance` — a cross-cutting layer (profiling, a centralized tick
system, pooling hardening, resource loading, mobile utilities, memory diagnostics, performance
budgets) that sits alongside `GameFramework.PlayerSystems`/`GameFramework.Gameplay`, not inside
either. Full API examples and design rationale live in
`Assets/GameFramework/Documentation/Framework.md`'s "Performance Infrastructure" section — this
section is the stable rule summary; that one is the living reference.

## Performance Architecture

* `GameFramework.Performance` references only `GameFramework.Core`/`GameFramework.Runtime` — never
  Input/UI/Audio/Feedback/Gameplay. It must stay usable by any game regardless of which other
  systems it also uses.
* `GameFramework.Gameplay` takes a one-way reference *on* `GameFramework.Performance` (pooling's
  profiling markers/statistics). Never the other direction — that would be a cycle.
* Registered services (`ITickService`, `IPerformanceMonitorService`, `IApplicationLifecycleService`,
  `IMobilePerformanceService`) are added by `PerformanceBootstrapper`, a `GameBootstrapper`
  subclass — the same registration-extension mechanism every other phase's bootstrapper subclass
  uses. Do not register them from `GameBootstrapper` itself.
* `MemoryDiagnostics` and `DeviceInfo` are static utilities, not services — they have no lifecycle,
  just point-in-time engine queries. Do not wrap them in a service for the sake of symmetry.
* Every frame-rate/budget number this layer works with is a **configured target**, checked against a
  **measured** value — never claim it as a guarantee. Real performance depends on the device,
  resolution, scene content, and thermal state.

## Tick Rules

* Use plain `MonoBehaviour.Update()` for anything simple, low-count, or already working. Reach for
  `ITickService` when a large/variable number of objects would otherwise each carry their own
  `Update()`, or when centralized enable/disable and ordering actually matter.
* `ITickable` ticks every frame regardless of gameplay pause (its `deltaTime` is simply 0 while
  paused, exactly like Unity's own `Update` + `Time.deltaTime`). Use
  `GameFramework.Gameplay.IGameplayTickable` instead when a paused gameplay session should stop the
  object being called at all.
* Fixed-timestep work goes through `IFixedTickable`/`RegisterFixed` — never simulate physics from
  the variable `ITickable` phase.
* `ILateTickable`/`RegisterLate` is for presentation work that must run after every `Tick` has moved
  things (camera follow, etc.) — do not move gameplay logic there merely for symmetry with
  Fixed/Late.
* Always pair `Register` with `Unregister` (`OnEnable`/`OnDisable` is the usual place) — an object
  that stops needing per-frame work but stays registered is a silent, avoidable cost.
* Do not call `ITickService`'s `Tick`/`TickFixed`/`TickLate` from game code — that is
  `GameBootstrapper`/`TickServiceDriver`'s job. (The Phase 5 benchmark scene is a deliberate,
  documented exception for measurement purposes only.)
* Do not add a second general-purpose tick/update system. If `ITickService` doesn't fit a case,
  that's a discussion, not a reason to build a competing one.

## Pooling Rules

* Pool objects that are repeatedly created/destroyed during gameplay (projectiles, effects, frequent
  UI elements). Do not pool everything — pooling adds lifecycle complexity that only pays for itself
  under real churn.
* Never `Object.Destroy`/`DestroyImmediate` a pooled instance directly — always `Release` it. A
  pool's `Release` now rejects a foreign object (one it never handed out via `Get`) rather than
  silently admitting it, so a direct-destroy-then-somehow-release bug surfaces as a logged error,
  not silent corruption.
* `GameObjectPool.Statistics` is a read-only development diagnostic — do not use it to drive
  gameplay logic (e.g. don't branch on `PeakActiveCount`); it exists for the performance
  overlay/development report, not as a public gameplay API.
* Use `PrewarmStagedRoutine` only when profiling shows a large `Prewarm` call actually causes a
  visible frame spike. For a handful of instances, plain `Prewarm` is simpler and just as cheap.
* Ownership: a pool's container Transform owns every instance it created. Disposing a pool while
  instances are still checked out leaves those references dangling (logged as a warning) — release
  everything you got from a pool before disposing it.

## Resource Rules

* The project does not use Addressables or AssetBundles. Do not introduce Addressables for a new
  feature unless there is a concrete project requirement — extend `IAssetProvider` instead, or use
  direct references, exactly as Phases 0–4 already do.
* Every `IAssetProvider.Load`/`LoadAsync` call returns a handle that must be released exactly once.
  A `Load` with no matching `Release` keeps that asset referenced indefinitely — this is on the
  caller, the provider does not guess when you're done with something.
* `LoadAsync`'s `owner` parameter exists specifically so a callback never fires into a destroyed
  object — pass it whenever the callback would touch a `MonoBehaviour`/scene object.
* Releasing the last handle for a key does not force-unload a `GameObject`/`Component` asset (Unity
  doesn't support that) — those become eligible for the next `Resources.UnloadUnusedAssets()`. Don't
  expect an immediate memory drop from `Release` alone for those types.

## Performance Rules

* **Profile first, optimize second.** Do not change code because it "looks slow" — use
  `ProfileScope`/the Unity Profiler/`IPerformanceMonitorService` to establish there's a real cost
  before changing anything for performance reasons.
* Hot paths (tick loops, pooling `Get`/`Release`, physics queries, input polling, spawning): no
  LINQ, no per-call allocation, no uncached `GetComponent`, no reflection, no `Find*`. This was
  already true of Phases 0–4 as of the Phase 5 audit — keep it true.
* `ProfileScope` is safe to leave in a hot path — it is a zero-allocation `readonly struct` gated by
  a single branch on `PerformanceSettings.Mode`, and costs nothing when `Disabled` (the default
  outside the Editor/development builds).
* Never call `GC.Collect()` during gameplay. If a truly exceptional scenario seems to need it,
  that's a design discussion first, not a quick fix.
* Report only measurements you actually obtained. Distinguish **Measured** (a real `Stopwatch`/
  Profiler/`IPerformanceMonitorService` reading), **Estimated** (e.g. `GC.GetTotalMemory`, which is
  an estimate by definition), and **Configured** (a target frame rate/budget) — never present one as
  another.

---

# 78. Phase 12 UI Navigation & Menu Flow Infrastructure

Phase 12 added `GameFramework.UI.Navigation` — UI navigation/menu-flow orchestration that sits above
Phase 3's `GameFramework.UI` (screens, popups, layered canvases), not inside it. Full API examples
and design rationale live in `Assets/GameFramework/Documentation/Framework.md`'s "UI Navigation &
Menu Flow Framework" section — this section is the stable rule summary; that one is the living
reference.

## Navigation Architecture

* `GameFramework.UI.Navigation` orchestrates Phase 3's screen/popup stack — it never instantiates,
  parents, shows, hides, or destroys a screen/popup GameObject itself. Every mutation goes through
  `IUIService.OpenScreen`/`OpenPopup`/`CloseScreen`/`ClosePopup`. Do not bypass `INavigationService`
  to call `IUIService` directly once Navigation is registered — its own stack bookkeeping (parameters,
  result callbacks, pause tokens) will desync from Phase 3's physical stack if you do.
* A screen/popup opened through `INavigationService` must also be closed through it
  (`NavigateBack`/`CloseTopPopup`), never through its own inherited `UIScreen.Close()`/`UIPopup.Close()`
  shortcut — that shortcut talks to `IUIService` directly and leaves a ghost entry in Navigation's
  stack.
* `INavigationService` is registered by `NavigationBootstrapper`, which subclasses
  `PlayerSystemsBootstrapper` directly (not `GameBootstrapper`) because it has a genuine hard
  dependency on `IUIService` — the same reasoning `QuestsBootstrapper` subclasses
  `ProgressionBootstrapper` for its own hard dependency on Rewards. Do not register
  `INavigationService` from a bare `GameBootstrapper` subclass without `IUIService` already
  registered — it will throw at `Initialize`.
* `Input.IInputService` (Android/back-button routing) and `GameFlow.IGameFlowService` (popup pause
  tokens) are resolved softly (`registry.TryGet`) — Navigation works fully without either registered,
  it just skips that one piece. Register them *before* `INavigationService` if you want them honored.

## Navigation Stack Rules

* Use `Navigate` to push a new screen, `Replace` to swap the current one without keeping it in
  history, and `Reset` to clear the whole stack and establish a new root (e.g. Main Menu -> Gameplay,
  where back should never return to the menu). Do not simulate `Reset` by calling `NavigateBack` in a
  loop — it doesn't invoke guards/results the same way and is not the intended API.
* Every command returns a `NavigationResult` instead of throwing. Check `.Kind`/`.Success` rather
  than assuming success — a duplicate/concurrent/blocked request is normal flow, not an exceptional
  one.
* A navigation command issued while `INavigationService.IsNavigating` is true is rejected
  (`AlreadyActive`), including one issued synchronously from inside a lifecycle hook
  (`OnOpened`/`OnHidden`/`OnShown`/`OnClosed`, or an `IUINavigationBackHandler` callback) that
  Navigation itself just triggered — the same deliberate re-entrancy guard `GameFlowService`/
  `TutorialService` already apply to their own event handlers. Defer such a follow-up call by one
  frame (a coroutine) instead of calling back in directly.
* Contexts are not a separate persisted stack per flow — Phase 3 destroys a closed screen's
  GameObject, so there's no seam to keep a hidden context's screens alive to resume exactly as left.
  Use `Reset` to move between top-level flows instead of inventing a context-switch mechanism.

## Back Navigation Rules

* Do not read `Input.GetKeyDown(KeyCode.Escape)` (or any other raw back-equivalent input) anywhere
  outside `NavigationBackButtonDriver` — that is the one centralized place this framework reads a
  physical back signal. Call `INavigationService.NavigateBack()` instead.
* `NavigateBack` always tries, in order: an open popup, then the current screen's own
  `IUINavigationBackHandler`, then a stack pop, then publishing `BackRequestedAtRootEvent`. Do not
  hard-code back-button handling into an individual screen/popup that bypasses this chain.
* This framework never calls `Application.Quit()` on your behalf, even when back-navigation reaches
  the root with nothing left to pop — it only publishes `BackRequestedAtRootEvent`. Deciding what
  "back at the root" means (confirm-exit popup, forward to GameFlow, quit) is a game decision.

## Popup Rules

* A popup's `NavigationRequestOptions.PausesGameplay` acquires a `GameFlow.IPauseToken` for exactly
  as long as it's open and releases it automatically on close, by any path. Do not also call
  `ITimeService.Pause`/`Resume` yourself for the same popup — that would double-pause/release
  incorrectly against the reference count.
* Popups stack independently of screens. Back-navigation always closes the topmost popup before
  touching the screen stack at all, regardless of how many popups are open.

## Parameters, Results, and Guards

* `NavigationRequestOptions.Parameters`/`ResultCallback` are deliberately untyped (`object`) — the
  call site is already type-safe (your own typed local is what gets boxed); the receiver
  (`IUINavigationParameterReceiver.OnNavigationParameters`) does one explicit cast. Do not add a
  generic `INavigationService.Navigate<T>` overload to "fix" this — it does not remove the cast on
  the receiving end and adds API surface for no real safety gain.
* A registered `INavigationGuard` runs for every screen/popup request, including back navigation.
  `NavigationGuardResult.Defer` does not queue or retry the request — the guard itself is responsible
  for calling the navigation method again once ready. Do not build a generic workflow/retry engine
  around this; it is explicitly out of scope.
* `IUINavigationTransitionHandler` is enter-only by design — Phase 3 destroys a closing screen/popup
  synchronously, so there is no seam to defer that destruction for an exit animation. Play an exit
  animation synchronously inside `UIScreen.OnClosed`/`OnHidden` instead of asking for an exit-transition
  hook here.

---

# 79. Phase 13 Save Profiles & Player Data Framework

Phase 13 added `GameFramework.PlayerData` — a player-profile and player-data orchestration layer on
top of Phase 2's `Runtime.Persistence.IPersistenceService`, not a second storage/serialization
system. Full API examples and design rationale live in
`Assets/GameFramework/Documentation/Framework.md`'s "Save Profiles & Player Data Framework" section —
this section is the stable rule summary; that one is the living reference.

## Player Data Architecture

* `GameFramework.PlayerData` references only `GameFramework.Core`/`GameFramework.Runtime` — never
  Input/UI/Audio/Feedback/Gameplay/GameFlow/Progression/Navigation/Performance. Every dependency
  `PlayerProfileService` needs (`IPersistenceService`, `IEventService`, `ITimerService`, optionally
  `ISceneService`/`ILoggingService`) is already part of the base eight services `GameBootstrapper`
  registers, so it must stay usable by any game regardless of which other systems it also uses.
* `IPlayerProfileService` is registered by `PlayerDataBootstrapper`, a `GameBootstrapper` subclass —
  not a `PlayerSystemsBootstrapper` subclass — since it has no hard dependency on Input/UI. Do not
  register it from a subclass that requires Phase 3 just to satisfy a dependency it doesn't have.
* Every section's data is its own `IPersistenceService` key
  (`"GameFramework.PlayerData.{profileId}.{sectionId}"`); a profile's metadata is a sibling key
  (`"...{profileId}.Meta"`); the list of existing profile ids is tracked under
  `"GameFramework.PlayerData.Index"`. Do not invent a second envelope/versioning format — every key
  still goes through Phase 2's existing `Save`/`Load`/`RegisterMigration`.
* `SettingsService`/`EconomyService`/`InventoryService`/`ExperienceService`/`StatisticsService`/
  `TutorialService` are **not** retrofitted onto profile-scoped keys — each already persists itself
  against a fixed, non-profile-scoped key, and changing that would silently move every existing
  game's save data. A game that wants one of those six systems' data inside a profile wraps it in its
  own `PlayerDataSection<TData>` adapter; do not modify those six services to do this automatically.

## Section Rules

* A game defines persistent data by subclassing `PlayerDataSection<TData>`, never by implementing
  `IPlayerDataSection` directly unless there is a concrete reason `PlayerDataSection<TData>`'s
  Save/Load/dirty-tracking/backup implementation doesn't fit.
* Call `MarkDirty()` from inside a section's own domain mutator only after the mutation actually
  changed something — a no-op mutator call must not mark dirty, since that would schedule/reschedule
  an unnecessary autosave.
* `Validate()` should repair whatever it safely can in place (clamp, drop a dangling reference).
  Throwing from it is reserved for state the section genuinely cannot make sense of — that is treated
  as corruption (`ProfileOperationResultKind.Corrupted`) during load, and skips that section's write
  (keeping it dirty for a retry) during save, rather than persisting or accepting bad data.
* `RegisterSection`/`RegisterMigration` must be called before any profile is loaded. Do not call
  either after `IPlayerProfileService.ActiveProfile` is non-null — both throw
  `InvalidOperationException` deliberately, the same "composition-root mistake, not a recoverable
  runtime condition" reasoning `IPersistenceService.RegisterMigration`'s duplicate-registration throw
  already establishes.

## Lifecycle and Concurrency Rules

* Every `IPlayerProfileService` command returns a `ProfileOperationResult` instead of throwing, and
  is rejected with `ProfileOperationResultKind.AlreadyActive` while another is in progress — including
  a call issued synchronously from inside this service's own event handlers. Defer such a follow-up
  call by one frame (a coroutine, or an `ITimerService` one-shot) instead of calling back in directly
  — the same re-entrancy rule `INavigationService` already documents.
* `ProfileState.Saving` always returns to `Loaded` regardless of the save's outcome. Do not add a
  separate stuck "SaveFailed" state — a failed save is reported through `LastSaveResult`/
  `ProfileSaveFailed`, never by blocking further use of the profile.
* Do not read `Application.OnApplicationPause`/`OnApplicationFocus`/`OnApplicationQuit` anywhere in
  Player Data code outside `PlayerDataLifecycleDriver` — that is the one centralized place this
  layer reads those callbacks. This driver is deliberately independent of
  `Performance.Mobile.IApplicationLifecycleService` (see its own remarks) — do not add a reference to
  `GameFramework.Performance` to "reuse" that service instead; that would force every Player Data user
  to pull in Performance's entire surface just for pause detection.
* Autosave scheduling goes through `Runtime.Timers.ITimerService` (unscaled, so it still counts down
  while gameplay is paused). Do not add an `Update()`/polling loop to check dirty state — a section
  notifies the service the instant it is marked dirty via an internal hook, never by being polled.
* A dirty-driven autosave request coalesces with any already-pending one (cancel-and-reschedule) —
  it does not queue a second save. Do not change this to a queue without a concrete reason; rapid
  mutations (a burst of coin pickups) must still produce one write, not several.

## Corruption / Backup Rules

* `EnableBackups` (on by default) copies a section's current on-disk data to a `.bak` companion key
  immediately before overwriting it during save — one extra load+save per dirty section per save
  cycle, never per mutation. Do not call this per mutation or per frame.
* On load, corruption detection relies on deleting any stale `.corrupt` marker for a key before
  calling `IPersistenceService.Load`, then checking whether a new one appears — `PersistenceService`
  only creates that marker when a load actually failed. Do not skip the delete-before-check step; a
  marker left over from a previous failed attempt would otherwise cause a load that succeeds this
  time to still be reported as corrupted.
* A failed section tries its own `.bak` first, then falls back to `ResetToDefaults()` — the profile
  still loads (`Corrupted`, not a hard failure). Never leave a section half-loaded, and never treat a
  corrupted section as a reason to fail the entire profile load.
* A missing migration chain (`IPersistenceService.Load` logging "no migration registered" and
  returning defaults) is a content/authoring bug to catch by keeping migrations registered for every
  shipped version — it is not distinguishable from "no save yet" through Phase 2's current public
  surface, and no additive Phase 2 API change was made to fix that; do not assume this layer detects
  it the same way it detects a genuine deserialize failure.
* A profile save is not multi-file-transactional — one section's `Save`/`Validate` failure does not
  roll back a different section's already-written data (Phase 2 has no multi-key transaction
  primitive). Do not build one for this; log, isolate, and report `ProfileOperationResultKind.Failed`
  with the affected section ids instead.

---

# 80. Phase 14 Mobile Platform & Device Services Framework

Phase 14 added `GameFramework.Platform` — platform identity, device information/capabilities,
screen/orientation/safe-area, clipboard, URL opening, app-store linking, network reachability, and a
minimal cross-platform permission model. Full API examples and design rationale live in
`Assets/GameFramework/Documentation/Framework.md`'s "Mobile Platform & Device Services Framework"
section — this section is the stable rule summary; that one is the living reference.

## Platform Architecture

* `GameFramework.Platform` references only `GameFramework.Core`/`GameFramework.Runtime` — never
  Input/UI/Audio/Feedback/Gameplay/Performance. It must stay usable by any game regardless of which
  other systems it also uses, the same independence Phase 5/Phase 13 already established for
  themselves.
* Application lifecycle (pause/resume/focus/quit) is **not** re-implemented here.
  `Performance.Mobile.IApplicationLifecycleService` (Phase 5) already centralizes exactly that; a
  game using both Platform and Performance gets one lifecycle relay, not two competing ones. A game
  that wants lifecycle visibility without the rest of Performance registers
  `Performance.Mobile.ApplicationLifecycleService` directly — Platform does not gate this behind its
  own bootstrapper.
* Haptics are **not** re-implemented here either. `Feedback.IHapticProvider`/`MobileHapticProvider`
  (Phase 3) already covers the one haptic API Unity exposes without a native plugin
  (`Handheld.Vibrate`); `DeviceInfoService.Supports(DeviceCapability.Haptics)` mirrors
  `MobileHapticProvider.IsSupported`'s exact same `Application.isMobilePlatform` check rather than
  inventing a different heuristic for the same question.
* `PlatformBootstrapper` is a `GameBootstrapper` subclass (not a `PlayerSystemsBootstrapper`
  subclass) — nothing it registers has a hard dependency on Input/UI. Registration order matters:
  `IDeviceInfoService` and `IAppStoreService`/`IAppSettingsService` resolve `IPlatformService` (and,
  for the store service, `IPlatformUrlService`) during their own `Initialize`, so those must already
  be registered.
* `DeviceInfoService` deliberately does not depend on `Performance.Mobile.DeviceInfo` (Phase 5's
  static utility). That type is a narrower, differently-scoped snapshot for picking a coarse
  `PerformanceProfile`; this service is the general-purpose device/capability surface a game may want
  without pulling in all of `GameFramework.Performance`. The resulting duplicated `SystemInfo` reads
  are one-time, at `Initialize`, never a hot-path concern — the same reasoning
  `PlayerDataLifecycleDriver` already documents for its own deliberate independence from Phase 5.

## Capability & Permission Rules

* Prefer `IDeviceInfoService.Supports(DeviceCapability)` over branching on `IPlatformService.Platform`
  for feature checks — a platform is not a reliable proxy for a capability, and a specific device may
  lack a feature its platform generally supports.
* `DeviceCapability` deliberately excludes Camera/Microphone hardware-presence checks — enumerating
  those devices touches sensitive platform surface for a passive capability query, which conflicts
  with this phase's privacy-minimal design. `PlatformPermission` covers the permission side of camera/
  microphone instead, and is deliberately limited to the two permissions Unity itself exposes a
  genuine cross-platform check/request API for
  (`Application.HasUserAuthorization`/`RequestUserAuthorization`). Do not add a permission this API
  can't actually answer (location, notifications, storage) without a real native plugin backing it.
* `IPermissionService` never shows an OS prompt on its own — only an explicit
  `RequestPermission` call does. Do not call `RequestPermission` from framework/bootstrap code; that
  decision belongs to the game.

## Screen & Native Integration Rules

* Unity has no change callback for `Screen.orientation`/`Screen.safeArea`. `ScreenService` is the one
  place that polls both, once per frame, through `ScreenSignalDriver`, and only publishes
  `ScreenOrientationChangedEvent`/`ScreenSafeAreaChangedEvent` when a value actually changed — do not
  add a second poller for either elsewhere.
* This phase does not add a UI-side safe-area component. `IScreenService.SafeArea` is the
  platform/device information a UI layer would consume; no such component exists yet in this project
  (see Framework.md's UI Foundation section) — do not build one here as a side effect of adding
  `IScreenService`.
* `Android/AndroidAppSettingsProvider` is the one place in this framework that touches
  `AndroidJavaObject`/`AndroidJavaClass` directly, gated `#if UNITY_ANDROID && !UNITY_EDITOR`. Do not
  scatter native Android calls anywhere else; add a new isolated provider under `Platform/Android/`
  (or `Platform/iOS/` for a future native iOS need) instead.
* `IAppStoreService`/`IPlatformUrlService` need no native code — the Play Store and App Store both
  expose ordinary URL schemes (`market://`, `itms-apps://`) opened through
  `Application.OpenURL`. Do not add an `AndroidJavaObject`-based store-opening path; it would
  duplicate a already-working, simpler mechanism.
* `AppStoreConfig` (Android package name, iOS App Store id) is optional, game-supplied configuration
  — never hard-code a project's store identifiers into framework code. Missing/unconfigured platforms
  fail gracefully (`OpenStorePage`/`OpenReviewPage` return `false` and log a warning), never throw.

---

# 81. Phase 15 Monetization: Ads, IAP & Entitlements Framework

Phase 15 added `GameFramework.Monetization` — a game-facing Ads/Purchases/Entitlements API so
gameplay/UI code never depends on Google Mobile Ads, Unity IAP, StoreKit, or Google Play Billing
directly. Full API examples and design rationale live in
`Assets/GameFramework/Documentation/Framework.md`'s "Monetization Framework" section — this section
is the stable rule summary; that one is the living reference. No ad or IAP SDK is installed in this
project (`Packages/manifest.json` has neither Google Mobile Ads nor `com.unity.purchasing`) — only
the provider seam and deterministic mock providers exist.

## Monetization Architecture

* `GameFramework.Monetization` references `GameFramework.Core`/`GameFramework.Runtime` (minimal-
  sibling shape) plus a genuine hard dependency on `GameFramework.Rewards` (a completed/restored
  purchase claims through `Rewards.IRewardService`/grants through this phase's own
  `IEntitlementService`) and a hard reference to `GameFramework.Performance` for the
  `ApplicationPausedEvent`/`ApplicationResumedEvent` struct types only, consumed through
  `IEventService` — never a `TryGet` lookup on `IApplicationLifecycleService` itself. Do not add a
  reference to Input/UI/Audio/Feedback/Gameplay from this assembly.
* `MonetizationBootstrapper` extends `Rewards.ProgressionBootstrapper` directly — the same reasoning
  `Quests.QuestsBootstrapper` already established for its own hard dependency on Rewards. It registers
  `IEntitlementService` before `IAdsService`/`IPurchaseService`, since both resolve it softly
  (`registry.TryGet`) during their own `Initialize`, which only succeeds once it is already
  registered and initialized. Do not reorder this registration.
* Ads, Purchases, and Entitlements are three separate interfaces, never one giant
  `IMonetizationService`. A purchase is a transaction/event; an entitlement is what the player
  currently owns; do not conflate the two or infer ownership from raw purchase history.
* `IAdsService` never references `Rewards.IRewardService`/`IEntitlementService` (grant side) at all —
  it only ever reports `RewardedAdResult`/publishes `AdRewardEarnedEvent`. `IPurchaseService`, by
  contrast, resolves both softly and grants automatically on a completed/restored purchase. Do not
  make `IAdsService` grant a reward directly, and do not remove `IPurchaseService`'s automatic grant
  in favor of a manual game-side step — see "Why Ads and Purchases integrate with Rewards
  differently" in Framework.md for the reasoning behind this asymmetry.
* No Google Mobile Ads or Unity IAP adapter exists in this project. Do not fabricate one against
  guessed APIs — if a game installs one of those packages, a new adapter assembly (referencing only
  that SDK) implements `IAdProvider`/`IPurchaseProvider` against the actually-installed version, and
  nothing in `AdsService`/`PurchaseService`/`MonetizationBootstrapper`'s public surface needs to
  change beyond which provider instance the bootstrapper constructs.

## Ads Rules

* Game code requests a stable `AdPlacementId` (e.g. `"RewardedRevive"`), never a raw provider ad-unit
  id — those live only in `AdConfiguration`/`AdPlacementConfig`, resolved per-platform via
  `ResolvePlatformUnitId()`.
* Only one Interstitial/Rewarded ad may be "showing" at a time. That mutual-exclusion check happens
  *before* `CanShow`'s cooldown/session-limit/entitlement-suppression policy check, in both `Show`
  and `ShowRewarded` — a fullscreen ad already on screen is a structural constraint, not a policy one,
  and must win even if the same placement would otherwise be back on cooldown the instant it's shown.
  Do not reorder these checks.
* A reward must only ever be granted for `RewardedAdResult.RewardEarned`, reported after
  `IAdProvider.AdRewardEarned` actually fires (always before that placement's `AdClosed`, never
  instead of it) — never merely because `ShowRewarded` was called. Do not grant on `AdShown`/`AdClosed`
  alone.
* Cooldown/session-limit are authored per placement (`AdPlacementConfig.CooldownSeconds`/
  `SessionShowLimit`, 0 = no limit) and checked via `ITimeService.Realtime` — never
  `UnityEngine.Time` directly, and never a game-specific universal rule like "every 3 levels" baked
  into the framework.
* Entitlement suppression (`AdConfiguration.EntitlementSuppressions`) maps an owned `EntitlementId`
  to the specific `AdType`s it suppresses. Do not assume Remove Ads suppresses Rewarded — that must
  stay an authored, explicit choice per rule, never a hard-coded default.
* `AdsService` hides active banners on `ApplicationPausedEvent` and re-shows them on
  `ApplicationResumedEvent` (both consumed via `IEventService`, not a new `MonoBehaviour` callback).
  Do not add a second place in the framework that reads `OnApplicationPause`/`OnApplicationFocus` for
  ad purposes.
* A load failure retries up to `AdConfiguration.MaxLoadRetries` times via `ITimerService`, backing off
  by `RetryBackoffSeconds * attemptNumber`. Do not poll for ad availability every frame.

## Purchase Rules

* Game code requests a stable, logical `ProductId` (e.g. `"remove_ads"`), never a store product id —
  those live only in `ProductCatalog`/`ProductDefinition`, resolved per-platform via
  `ResolvePlatformProductId()`.
* Purchase idempotency is mandatory and already implemented: every granted transaction id is recorded
  in a persisted set (`PurchaseSaveData`, its own `IPersistenceService` key) before
  `PurchaseService.ProcessGrant` ever calls `Rewards.IRewardService.TryClaim`/
  `IEntitlementService.GrantEntitlement` — a duplicate provider callback for an already-processed
  transaction id must report the same outcome without granting again. Do not weaken this to a
  best-effort check, and do not claim it survives a crash mid-write — Phase 2's `IPersistenceService`
  offers no atomic-transaction primitive, and `PurchaseSaveData`'s own remarks document that
  limitation explicitly.
* A completed or restored purchase automatically grants `ProductDefinition.GrantedEntitlementId`/
  `GrantedRewardId` — this is deliberate (a product's grant is fixed, authored data, not a
  gameplay-time decision) and different from how Ads integrates with Rewards (see "Ads Rules" above).
  Do not add a seam for a game to intercept/override this grant; if a product needs conditional
  logic, that belongs in the reward/entitlement content itself (`IReward.CanGrant`), not in
  `PurchaseService`.
* `IPurchaseValidator`/`LocalPurchaseValidator` are the validation *seam*, not real security. Never
  claim client-side validation prevents fraud — a future server-backed `IPurchaseValidator`
  implementation is the intended extension point, and this phase deliberately does not build a
  backend.
* `RestorePurchases` grants each restored product under a `"restore:{productId}"` dedupe key (a
  provider isn't guaranteed to supply a real per-product transaction id on restore). Do not assume
  this makes restore fully re-grant-capable after a manual revoke — that's a documented, accepted
  scope limitation, not a bug to "fix" by removing the dedupe.

## Entitlement Rules

* `IEntitlementService` persists directly through `IPersistenceService` (`EntitlementSaveData`, its
  own key) — the same pattern `Rewards.RewardService`/`Unlocks.UnlockService` already use. Do not
  retrofit it onto a `PlayerData` profile section; a game wanting profile-scoped entitlements wraps
  this service's data in its own `PlayerDataSection<TData>` adapter, the same non-retrofit precedent
  Phase 13 already established for its own six self-persisting systems.
* `HasEntitlement` accounts for expiration (`EntitlementState.IsCurrentlyActive`); `GetEntitlement`
  does not. A lapsed subscription is "owned but not currently active," never "never owned" — do not
  conflate the two when building UI that distinguishes a "renew" prompt from a "buy" prompt.
* `SyncFromProvider` never revokes an id the provider simply didn't report on. A real store restore
  is non-exhaustive by nature (e.g. it does not re-list an already-consumed consumable) — "not
  reported" is not the same claim as "not owned." Do not add logic that revokes everything not
  present in a restore's result set.

## Provider & Testing Rules

* `Providers.Mock.MockAdProvider`/`MockPurchaseProvider` are the only shipped `IAdProvider`/
  `IPurchaseProvider` implementations. They must never be able to grant a real purchase or represent
  a real ad network — keep them deterministic and clearly Editor/test-scoped.
* `AdsServiceTests`/`PurchaseServiceTests` use fully-controllable `FakeAdProvider`/
  `FakePurchaseProvider` test doubles (not the shipped mocks) specifically so multi-step scenarios
  (a fullscreen ad already showing, a duplicate transaction callback, a deferred purchase resolving
  later) are deterministic to set up. The shipped mocks get their own separate `MockProviderTests`
  coverage. Do not conflate the two roles when adding new tests.
* Do not call `GameBootstrapper.Instance` from inside `GameFramework.Monetization` runtime code —
  every service is resolved through the `IServiceRegistry` passed to `Initialize`, the same rule
  every other phase's services already follow. `Editor.Monetization.MonetizationDiagnosticsMenu` is
  the one place allowed to reach for `GameBootstrapper.Instance` (development-time-only, mirroring
  `PlatformDiagnosticsMenu`/`PlayerDataDiagnosticsMenu`'s existing precedent).

---

# 82. Phase 16 Analytics + Crash / Diagnostics Framework

Phase 16 added `GameFramework.Analytics` — provider-independent analytics event tracking, session/
screen tracking, consent, and anonymous identity — plus `GameFramework.Analytics.Diagnostics`
(same assembly, separate namespace) — breadcrumbs, diagnostic context/tags, and a crash-reporting
provider boundary. A third, optional assembly, `GameFramework.Analytics.Integration`, holds four
opt-in bridges that forward GameFlow/UI Navigation/Tutorial/Monetization events into Analytics. Full
API examples and design rationale live in `Assets/GameFramework/Documentation/Framework.md`'s
"Analytics & Diagnostics Framework" section — this section is the stable rule summary; that one is
the living reference. No analytics or crash-reporting SDK is installed in this project
(`Packages/manifest.json` has neither Firebase, GameAnalytics, Unity Analytics, Sentry, nor
Crashlytics) — only the two provider seams plus deterministic No-Op/Mock providers exist.

## Analytics/Diagnostics Architecture

* `GameFramework.Analytics` references only `GameFramework.Core`/`GameFramework.Runtime` plus
  `GameFramework.Performance` (hard, for `ApplicationPausedEvent`/`ApplicationResumedEvent`/
  `ApplicationQuittingEvent` — the same "subscribe directly, no game required to have Performance's
  lifecycle service registered" pattern `Monetization.Ads.AdsService` already established) and
  `GameFramework.Platform` (hard, for diagnostic device/platform context only). It must stay usable
  by any game regardless of which other systems it also uses.
* `GameFramework.Analytics.Integration` is a **separate, optional** assembly layered on top —
  referencing `GameFramework.Analytics` + `GameFramework.GameFlow`/`GameFramework.UI.Navigation`/
  `GameFramework.Tutorials`/`GameFramework.Monetization` — rather than pulling those four references
  into the core Analytics assembly itself. This is deliberately different from how Presentation
  (Phase 10) or UI Navigation (Phase 12) broke the minimal-sibling pattern: those integrations are
  functionally required by the orchestration they perform, while GameFlow/Navigation/Tutorial/
  Monetization integration is purely optional instrumentation a game may not want at all — the same
  reasoning that gave Cinemachine (Phase 11) its own optional sibling assembly rather than making
  `GameFramework.Cameras` itself depend on Cinemachine. A game that doesn't reference
  `GameFramework.Analytics.Integration` still gets full Analytics/Diagnostics functionality.
* Analytics ("what happened in the game?") and Diagnostics ("what went wrong technically?") are
  deliberately two different interfaces (`IAnalyticsService`, `IDiagnosticsService`), not one giant
  service — see this brief's own section 5. There is no separate `ICrashReportingService`; crash
  reporting is a provider boundary (`ICrashReportingProvider`) that `IDiagnosticsService` forwards
  to internally, matching this brief's own section 3 diagram exactly (`IDiagnosticsService` →
  `ICrashReportingProvider` → external SDK) rather than adding a third public service interface for
  no additional boundary value.
* `AnalyticsBootstrapper` is a `GameBootstrapper` subclass (not a `PlayerSystemsBootstrapper`/
  `ProgressionBootstrapper` subclass) — neither `AnalyticsService` nor `DiagnosticsService` has a
  hard dependency on Input/UI/Rewards; both only require `IEventService`/`IPersistenceService`,
  already part of the base eight services `GameBootstrapper` registers. Registers
  `IDiagnosticsService` before `IAnalyticsService` — the latter resolves the former softly
  (`registry.TryGet`) during its own `Initialize`.
* Integration bridges (`Analytics.Integration.*AnalyticsIntegration`) are opt-in, plain `IDisposable`
  C# classes — never registered by `AnalyticsBootstrapper` — the same "opt-in, not bootstrapper-
  registered" precedent `Monetization.Integration.AdPlacementRewardBridge` already established. A
  game constructs the ones it wants once every relevant service is registered, and disposes them on
  shutdown. A bridge depends only on the *event types* it subscribes to (`IEventService.Subscribe<TEvent>`)
  — never on the publishing service itself — so it works whether or not that service ends up
  registered in a given game.

## Analytics Rules

* Game code calls `IAnalyticsService.Track(name, parameters)`. Never throws — an invalid event name
  or unsupported parameter is logged and dropped/sanitized, never raised as an exception (section 82
  — analytics must never crash the game). `AnalyticsService.SanitizeParameters`/`IsValidEventName`
  are the one enforcement point; a provider adapter may still apply its own, stricter rules on top.
* `AnalyticsEvent.Parameters` values are restricted to `string`/`int`/`long`/`float`/`double`/`bool`
  — anything else is dropped with a single aggregated warning per `Track` call, not one per
  parameter (avoids log spam — section 61). An event name must start with a letter and contain only
  letters/digits/`_`, matching common provider conventions (e.g. Firebase).
* Consent (`ConsentState`: `Unknown`/`Granted`/`Denied`) gates dispatch: `Denied` always drops
  immediately, never buffers; `Unknown` buffers (bounded, drop-oldest) or drops per the configured
  `ConsentPolicy`; `Granted` sends immediately. Denying consent after events were buffered clears the
  buffer without ever sending it — privacy takes precedence over completeness (section 37). This
  framework never builds a consent UI; a game's own UI/legal layer decides when to call
  `SetConsent`.
* Identity (`IAnalyticsService.UserId`) is an application-generated anonymous GUID persisted directly
  via `IPersistenceService` (its own key, `"GameFramework.Analytics.Identity"`) — deliberately **not**
  retrofitted onto a `PlayerData` profile section, the same non-retrofit precedent Phase 13/15
  already established for their own self-persisting systems. Never a device id/advertising id/
  hardware serial. `ResetIdentity()` is never called automatically by this framework.
* Session lifecycle is built on `Performance.Mobile.ApplicationPausedEvent`/`ApplicationResumedEvent`/
  `ApplicationQuittingEvent`, using wall-clock `DateTime.UtcNow` (not `Time.realtimeSinceStartup`,
  which does not advance while a mobile process is genuinely suspended in the background) to measure
  how long the app was backgrounded against `AnalyticsConfiguration.SessionTimeoutSeconds`. Do not
  assume `ApplicationQuittingEvent` always fires on mobile — a process can be killed by the OS
  without it; session-end there is best-effort, not guaranteed (section 16).
* `TrackScreenView` is a thin wrapper over `Track(EventNames.ScreenView, ...)` with a `screen_name`
  parameter — it takes a logical screen id, never a Unity scene name (section 18).

## Diagnostics Rules

* `IDiagnosticsService.RecordException`/`RecordError` always log through the existing
  `Runtime.Diagnostics.Log` facade regardless of whether diagnostics/a crash provider is enabled —
  the existing logger is never replaced (section 27). Only forwarded to `ICrashReportingProvider`
  when `DiagnosticsConfiguration.Enabled` is true.
* **No recursive diagnostics (mandatory, section 83):** `DiagnosticsService.Report`'s catch block
  around `ICrashReportingProvider.Report` logs the failure only through the plain `Log` facade — it
  never calls `RecordException`/`RecordError`/`Report` again. This is a structural guarantee (the
  catch block simply doesn't contain such a call), not a runtime flag to reason about.
* Breadcrumbs (`BreadcrumbRingBuffer`) are a fixed-capacity circular buffer
  (`DiagnosticsConfiguration.BreadcrumbCapacity`, oldest dropped once full) — never an unbounded
  list. Context (`SetContext`) and tags (`SetTag`) are plain `Dictionary<string,string>`, populated
  from `Platform.IPlatformService`/`IDeviceInfoService` (resolved softly) plus whatever an
  integration bridge or game sets.
* `UnhandledExceptionDriver` is the **one** place in this framework that reads
  `UnityEngine.Application.logMessageReceived` — do not add a second listener elsewhere. Its
  re-entrancy flag exists because `RecordError` itself logs through `Log`, which re-invokes this same
  callback synchronously; the flag makes that re-entrant call a no-op rather than recursing.
* Do not collect device serial numbers, precise location, or advertising ids as diagnostic context —
  only `framework_version`/`app_version`/`platform`/`device_model`/`operating_system` plus whatever a
  game explicitly sets via `SetContext`/`SetTag` (section 32/56).

## Provider Rules

* `IAnalyticsProvider`/`ICrashReportingProvider` are the only two provider seams. No Firebase/
  GameAnalytics/Unity Analytics/Sentry/Crashlytics adapter exists — do not fabricate one against
  guessed APIs; a future adapter lives in its own assembly (e.g.
  `GameFramework.Analytics.Firebase`), referencing only that installed SDK, implementing one of the
  two provider interfaces against its real, installed API.
* `NoOpAnalyticsProvider`/`NoOpCrashReportingProvider` are `AnalyticsBootstrapper`'s defaults — a
  production build must not accidentally use `Providers.Mock.MockAnalyticsProvider`/
  `Diagnostics.Mock.MockCrashReportingProvider` (section 54); the bootstrapper's two Mock-provider
  inspector toggles exist for local development/testing only.
* `AnalyticsService`/`DiagnosticsService` wrap every provider call in a try/catch; a provider
  exception is caught, logged, and (for Analytics) reported through `IDiagnosticsService` as
  `ErrorCategory.Framework` when one is registered — never allowed to propagate out of `Track`/
  `RecordException`/etc.
* Do not call `GameBootstrapper.Instance` from inside `GameFramework.Analytics`/
  `GameFramework.Analytics.Integration` runtime code — every service is resolved through the
  `IServiceRegistry` passed to `Initialize`/the bridge constructor, the same rule every other phase's
  services already follow. `Editor.Analytics.AnalyticsDiagnosticsMenu`/`AnalyticsEventSimulatorWindow`
  are the only places allowed to reach for `GameBootstrapper.Instance` (development-time-only,
  mirroring `MonetizationDiagnosticsMenu`'s existing precedent).
