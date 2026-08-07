/// <summary>
/// Identifica un oggetto gestibile da una <see cref="ComponentPool{T}"/>.
/// Implementare direttamente solo se non si puo' ereditare da <see cref="PooledBehaviour"/>.
/// </summary>
public interface IPoolable
{
    /// <summary>La pool proprietaria. Assegnata dalla pool alla creazione, mai dal chiamante.</summary>
    IPoolReleaser Releaser { get; set; }

    /// <summary>True mentre l'oggetto e' prestato. Gestita dalla pool: rende Release() idempotente.</summary>
    bool IsInUse { get; set; }

    /// <summary>Chiamata dalla pool quando l'oggetto viene prestato.</summary>
    void OnAcquired();

    /// <summary>Chiamata dalla pool quando l'oggetto rientra. Deve riportarlo a uno stato pulito.</summary>
    void OnReleased();
}
