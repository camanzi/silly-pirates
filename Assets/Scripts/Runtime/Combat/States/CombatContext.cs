using System.Collections.Generic;

public class CombatContext
{
    public AbilityBase selectedAbility;
    public TargetingData targetingData = TargetingData.Empty;
    public void ClearCtx()
    {
        selectedAbility = null;
        targetingData = TargetingData.Empty;
    }
}