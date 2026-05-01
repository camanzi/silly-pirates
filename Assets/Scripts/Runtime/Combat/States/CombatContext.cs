using System.Collections.Generic;

public class CombatContext
{
    public AbilityBase SelectedAbility;
    public object AbilityCache;
    public TargetingData TargetingData = TargetingData.Empty;
    public void ClearCtx()
    {
        SelectedAbility = null;
        AbilityCache = null;
        TargetingData = TargetingData.Empty;
    }
}