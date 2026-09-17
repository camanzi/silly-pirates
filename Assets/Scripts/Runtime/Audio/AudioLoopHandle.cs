using System;

/// <summary>
/// A reference to a looping sound, minted by the caller and used to stop it.
///
/// It is a GUID and not a pool slot index: the handle is a key into a dictionary on the AudioDirector, so
/// a duplicate or late stop is simply a miss (a safe no-op) and can never hit a different voice recycled
/// into the same slot. The ABA problem is avoided by construction, with no need for a generation
/// counter.
/// </summary>
public readonly struct AudioLoopHandle : IEquatable<AudioLoopHandle>
{
    public readonly Guid Id;

    private AudioLoopHandle(Guid id) => Id = id;

    public static AudioLoopHandle New() => new(Guid.NewGuid());

    public static readonly AudioLoopHandle None = default;

    public bool IsValid => Id != Guid.Empty;

    public bool Equals(AudioLoopHandle other) => Id.Equals(other.Id);

    public override bool Equals(object obj) => obj is AudioLoopHandle other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(AudioLoopHandle a, AudioLoopHandle b) => a.Equals(b);

    public static bool operator !=(AudioLoopHandle a, AudioLoopHandle b) => !a.Equals(b);
}
