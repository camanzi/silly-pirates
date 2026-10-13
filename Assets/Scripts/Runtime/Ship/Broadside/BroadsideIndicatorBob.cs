using PrimeTween;
using UnityEngine;

/// <summary>
/// The slow up/down float of a Broadside zone indicator: a looping Yoyo tween on the pivot's local Y, so there
/// is no per-frame code. Scaled time on purpose: the pause (timeScale 0) freezes it.
/// </summary>
public class BroadsideIndicatorBob : MonoBehaviour
{
    [Tooltip("The transform that floats. Its local position at enable time is the rest position")]
    [SerializeField] private Transform _pivot;
    [Tooltip("Height of the float above the rest position, in world units")]
    [SerializeField] [Min(0f)] private float _amplitude = 0.15f;
    [Tooltip("Seconds for a full up-and-down cycle")]
    [SerializeField] [Min(0.01f)] private float _period = 2f;
    [Tooltip("Starts each indicator at a random point of its cycle, so neighbouring areas do not bob in sync")]
    [SerializeField] private bool _randomPhase = true;

    private Tween _tween;
    private float _restY;
    private bool _hasRest;

    private void OnEnable()
    {
        if (_pivot == null) return;

        // Captured once: re-enabling must not stack the offset of a tween stopped halfway.
        if (!_hasRest)
        {
            _restY = _pivot.localPosition.y;
            _hasRest = true;
        }

        Vector3 rest = _pivot.localPosition;
        rest.y = _restY;
        _pivot.localPosition = rest;

        // Same desync scheme as BaseEnemyIdleAnimationSO: a random start delay within one cycle.
        _tween = Tween.LocalPositionY(_pivot, _restY + _amplitude, _period * 0.5f, Ease.InOutSine,
            cycles: -1, cycleMode: CycleMode.Yoyo,
            startDelay: _randomPhase ? Random.Range(0f, _period) : 0f);
    }

    private void OnDisable() => _tween.Stop();
}
