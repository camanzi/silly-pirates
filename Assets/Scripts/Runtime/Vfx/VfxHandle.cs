using System;

/// <summary>
/// Riferimento a un VFX persistente, coniato dal chiamante e usato per fermarlo.
///
/// Stessa scelta di <see cref="AudioLoopHandle"/>: e' un GUID e non un indice di slot del pool,
/// quindi uno stop duplicato o tardivo e' un semplice miss (no-op sicuro) e non puo' mai colpire
/// un effetto diverso riciclato nello stesso slot.
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
