using System;

/// <summary>
/// A reference to a persistent VFX, minted by the caller and used to stop it.
///
/// The same choice as <see cref="AudioLoopHandle"/>: it is a GUID and not a pool slot index, so a
/// duplicate or late stop is simply a miss (a safe no-op) and can never hit a different effect recycled
/// into the same slot.
/// </summary>
public readonly struct VfxHandle : IEquatable<VfxHandle>
{
    public readonly Guid Id;

    private VfxHandle(Guid id) => Id = id;

    public static VfxHandle New() => new(Guid.NewGuid());

    public static readonly VfxHandle None = default;

    public bool IsValid => Id != Guid.Empty;

    public bool Equals(VfxHandle other) => Id.Equals(other.Id);

    public override bool Equals(object obj) => obj is VfxHandle other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(VfxHandle a, VfxHandle b) => a.Equals(b);

    public static bool operator !=(VfxHandle a, VfxHandle b) => !a.Equals(b);
}
