namespace Game.Combat
{
    /// <summary>Real time passed since the last frame, in seconds. Behind an interface so tests can drive it.</summary>
    internal interface ITime
    {
        float DeltaTime { get; }
    }
}
