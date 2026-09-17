using System.Threading;
using UnityEngine;

/// <summary>
/// The single source of truth for the state of the combat intro sequence.
/// Enemies read <see cref="IsIntroActive"/> to suppress their automatic spawn animation; the
/// <see cref="TurnController"/> waits on <see cref="IsCombatReady"/> before starting the game loop.
/// </summary>
[CreateAssetMenu(fileName = "CombatIntroState", menuName = "Combat/Intro/Combat Intro State")]
public class CombatIntroStateSO : ScriptableObject, ICombatSessionResettable
{
    public bool IsIntroActive { get; private set; }
    public bool IsCombatReady { get; private set; }

    // A "pass-through" default: a scene with no CombatIntroSequencer must never block.
    private void OnEnable() => ResetToPassthrough();

    // Guards against the previous scene's CombatIntroSequencer being destroyed without completing the
    // intro: without this reset IsCombatReady would stay false forever, and the new combat's
    // TurnController.WaitUntilCombatReadyAsync would never get going.
    public void ResetForNewCombat() => ResetToPassthrough();

    private void ResetToPassthrough()
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
