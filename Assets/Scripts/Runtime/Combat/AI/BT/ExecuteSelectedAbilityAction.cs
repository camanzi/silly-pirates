using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Execute Selected Ability",
    story: "[Agent] executes [SelectedAbility] on [SelectedTarget] via [TurnControllerRef]",
    category: "Enemy AI",
    id: "b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7")]
public partial class ExecuteSelectedAbilityAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<MonoBehaviour> Agent;
    [SerializeReference] public BlackboardVariable<AbilityBase> SelectedAbility;
    [SerializeReference] public BlackboardVariable<MonoBehaviour> SelectedTarget;
    [SerializeReference] public BlackboardVariable<MonoBehaviour> TurnControllerRef;

    protected override Status OnStart()
    {
        var hostile = Agent?.Value as HostileCharacter;
        var ability = SelectedAbility?.Value;
        var turnController = TurnControllerRef?.Value as TurnController;

        if (hostile == null || ability == null || turnController == null)
        {
            LogFailure("Missing agent, ability, or turn controller.");
            return Status.Failure;
        }

        ITargettable target = SelectedTarget?.Value as ITargettable;
        TargetingData targeting = BuildTargetingData(target);

        object cache = null;
        if (!ability.CanExecute(hostile, targeting, ref cache))
        {
            LogFailure("Ability cannot execute.");
            return Status.Failure;
        }

        ICommand command = ability.CreateCommand(hostile, targeting, ref cache);
        if (command == null)
        {
            LogFailure("CreateCommand returned null.");
            return Status.Failure;
        }

        turnController.AddCommand(command);
        return Status.Success;
    }

    private static TargetingData BuildTargetingData(ITargettable target)
    {
        if (target == null)
            return TargetingData.Empty;

        Vector3 worldPos = target.Transform.position;
        Vector3Int cellPos = (target as GridElement)?.gridPosition ?? Vector3Int.FloorToInt(worldPos);

        return new TargetingData(worldPos, cellPos, isOverValidGrid: true, selectedTarget: target);
    }
}
