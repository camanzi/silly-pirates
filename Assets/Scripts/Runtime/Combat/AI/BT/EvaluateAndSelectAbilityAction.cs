using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Evaluate And Select Ability",
    story: "[Agent] evaluates abilities and selects the best one using [AIData]",
    category: "Enemy AI",
    id: "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6")]
public partial class EvaluateAndSelectAbilityAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<MonoBehaviour> Agent;
    [SerializeReference] public BlackboardVariable<EnemyAIDataSO> AIData;
    [SerializeReference] public BlackboardVariable<AbilityBase> SelectedAbility;
    [SerializeReference] public BlackboardVariable<MonoBehaviour> SelectedTarget;

    protected override Status OnStart()
    {
        var hostile = Agent?.Value as HostileCharacter;
        var aiData = AIData?.Value;

        if (hostile == null || aiData == null)
        {
            LogFailure("Agent or AIData is null.");
            return Status.Failure;
        }

        var context = new AIContext(hostile, aiData.TurnOrderData, aiData.GridStateData);

        float bestScore = float.NegativeInfinity;
        EnemyAbilityBase bestAbility = null;
        TargetingData bestTargeting = default;

        foreach (var ability in aiData.Abilities)
        {
            if (ability == null) continue;
            float score = ability.Score(context, out TargetingData targeting);
            if (score > bestScore)
            {
                bestScore = score;
                bestAbility = ability;
                bestTargeting = targeting;
            }
        }

        if (bestAbility == null || bestScore <= float.NegativeInfinity)
        {
            LogFailure("No valid ability found.");
            return Status.Failure;
        }

        SelectedAbility.Value = bestAbility;
        SelectedTarget.Value = bestTargeting.selectedTarget as MonoBehaviour;

        return Status.Success;
    }
}
