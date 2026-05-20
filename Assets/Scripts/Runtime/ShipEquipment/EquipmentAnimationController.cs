using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(ShootingEquipment))]
public class EquipmentAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _meshHolder;
    private IAwakable _awakable;
    private Tween _pumpTween;
    private Vector3 _originalScale;
    private int _previousAwakeningPoints;

    private void Awake()
    {
        _awakable = GetComponent<IAwakable>();
        if (_meshHolder != null)
            _originalScale = _meshHolder.localScale;
    }

    private void OnEnable()
    {
        if (_awakable != null)
        {
            _previousAwakeningPoints = _awakable.CurrentAwakeningPoints;
            _awakable.OnAwakeningCountersChanged += OnCountersChanged;
        }
    }

    private void OnDisable()
    {
        if (_awakable != null)
            _awakable.OnAwakeningCountersChanged -= OnCountersChanged;
    }

    private void OnCountersChanged()
    {
        if (_awakable.CurrentAwakeningPoints > _previousAwakeningPoints)
            PlayPump();
        _previousAwakeningPoints = _awakable.CurrentAwakeningPoints;
    }

    private void PlayPump()
    {
        _pumpTween.Stop();
        _meshHolder.localScale = _originalScale;
        _pumpTween = Tween.Scale(_meshHolder, startValue: _originalScale, endValue: _originalScale * 1.3f,
            duration: 0.1f, ease: Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Rewind);
    }
}
