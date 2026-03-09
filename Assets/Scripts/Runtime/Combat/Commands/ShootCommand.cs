using System;
using System.Collections.Generic;
using UnityEngine;

public class ShootCommand : ICommand
{
    private readonly GridEquipment _equipment;

    private readonly ICollection<IDamageable> _targets;

    public ShootCommand(GridEquipment equipment, ICollection<IDamageable> targets)
    {
        _equipment = equipment;
        _targets = targets;

        equipment.isSelected = false;
        equipment.RemoveSelectionIndicator();
    }

    public async Awaitable ExecuteAsync()
    {
        foreach (IDamageable target in _targets)
        {
            await target.TakeDamage(_equipment.damage, _equipment.destroyCancellationToken);
        }
    }

    public void Undo()
    {
        throw new NotImplementedException();
    }
}
