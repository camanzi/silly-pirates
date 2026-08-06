using System.Threading;
using UnityEngine;

/// <summary>
/// Unico punto di verità sullo stato della sequenza di intro al combattimento.
/// I nemici consultano <see cref="IsIntroActive"/> per sopprimere la loro animazione di spawn
/// automatica; il <see cref="TurnController"/> attende <see cref="IsCombatReady"/> prima di
/// avviare il game loop.
/// </summary>
[CreateAssetMenu(fileName = "CombatIntroState", menuName = "Combat/Intro/Combat Intro State")]
public class CombatIntroStateSO : ScriptableObject
{
    public bool IsIntroActive { get; private set; }
    public bool IsCombatReady { get; private set; }

    // Default "passante": una scena senza CombatIntroSequencer non deve mai bloccarsi.
    private void OnEnable()
    {
        IsIntroActive = false;
        IsCombatReady = true;
    }

    public void BeginIntro()
    {
        IsIntroActive = true;
        IsCombatReady = false;
    }

    public void CompleteIntro()
    {
        IsIntroActive = false;
        IsCombatReady = true;
    }

    public async Awaitable WaitUntilCombatReadyAsync(CancellationToken token)
    {
        while (!IsCombatReady)
        {
            token.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync(token);
        }
    }
}
