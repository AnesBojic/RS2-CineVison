namespace CineVision.Model.Enums
{
    /// <summary>
    /// Persisted lifecycle state of a movie. The state machine's Initial state is a code-only
    /// entry point for inserts and is never stored, so it has no value here.
    /// </summary>
    public enum MovieLifecycleState
    {
        Draft = 0,
        Active = 1
    }
}
