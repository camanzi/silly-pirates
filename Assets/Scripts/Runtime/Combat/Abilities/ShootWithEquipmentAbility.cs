using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Equipment/Abilities/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : EquipmentAbilityBase {

    public override bool CanExecute(GridElement caster, ICollection<IDamageable> targets)
    {
        throw new System.NotImplementedException();
    }

    public override ICommand CreateCommand(GridElement caster, ICollection<IDamageable> targets)
    {
        throw new System.NotImplementedException();
    }
}
