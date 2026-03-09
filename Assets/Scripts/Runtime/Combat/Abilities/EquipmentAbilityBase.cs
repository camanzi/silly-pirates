using System.Collections.Generic;
using UnityEngine;

public abstract class EquipmentAbilityBase : AbilityBase {
    public abstract ICommand CreateCommand(GridElement caster, ICollection<IDamageable> targets);
    
    public abstract bool CanExecute(GridElement caster, ICollection<IDamageable> targets);
}
