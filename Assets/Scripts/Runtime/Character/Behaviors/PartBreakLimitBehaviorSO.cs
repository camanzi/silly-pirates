using UnityEngine;

[CreateAssetMenu(fileName = "PartBreakLimitBehavior", menuName = "Combat/Health Behaviors/Part Break Limit")]
public class PartBreakLimitBehaviorSO : HealthBehaviorSO
{
    [SerializeField] private int _maxBreaks;

    private int _breakCount;

    public override void OnDeath(HealthController controller)
    {
        if (++_breakCount >= _maxBreaks)
            controller.gameObject.SetActive(false);
    }
}
