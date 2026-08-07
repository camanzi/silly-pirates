using System;

/// <summary>
/// Riferimento a un suono in loop, coniato dal chiamante e usato per fermarlo.
///
/// E' un GUID e non un indice di slot del pool: l'handle e' la chiave di un dizionario
/// sull'AudioDirector, quindi uno stop duplicato o tardivo e' un semplice miss (no-op sicuro)
/// e non puo' mai colpire una voce diversa riciclata nello stesso slot. Il problema ABA
/// e' evitato per costruzione, senza bisogno di un generation counter.
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
