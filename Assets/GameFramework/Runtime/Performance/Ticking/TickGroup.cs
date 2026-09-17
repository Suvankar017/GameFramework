namespace GameFramework.Performance.Ticking
{
    /// <summary>
    /// Optional, informational tag recorded alongside a tick registration - a small, fixed set
    /// deliberately (not a general dependency-graph system). It does not change execution order by
    /// itself (see <see cref="ITickService"/>'s priority parameter for that); it exists so
    /// diagnostics (the performance overlay, a development report) can break tickable counts down
    /// by what kind of work they represent.
    /// </summary>
    public enum TickGroup
    {
        Gameplay,
        Physics,
        AI,
        Animation,
        Presentation
    }
}
