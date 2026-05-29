using PrimeTween;
using UnityEngine;

public class SpeedBoostCommand : ICommand
{
    private readonly HostileCharacter _caster;
    private readonly HostileCharacter _target;
    private readonly SpeedBoostPassiveSO _passiveSO;

    public SpeedBoostCommand(HostileCharacter caster, HostileCharacter target, SpeedBoostPassiveSO passiveSO)
    {
        _caster = caster;
        _target = target;
        _passiveSO = passiveSO;
    }

    public async Awaitable ExecuteAsync()
    {
        var ct = _caster.Transform;
        Vector3 origin = ct.position;

        await Tween.Position(ct, origin + Vector3.forward,      0.1f,  Ease.OutQuad);
        await Tween.Position(ct, origin - Vector3.forward,      0.1f,  Ease.OutQuad);
        await Tween.Position(ct, origin + Vector3.right,        0.1f,  Ease.OutQuad);
        await Tween.Position(ct, origin - Vector3.right,        0.1f,  Ease.OutQuad);
        await Tween.Position(ct, origin,                        0.1f,  Ease.OutQuad);

        await Tween.Position(ct, origin + Vector3.up * -0.25f,  0.08f, Ease.OutQuad);
        await Tween.Position(ct, origin + Vector3.up *  1.25f,  0.20f, Ease.OutBack);
        await Tween.Position(ct, origin,                        0.15f, Ease.InQuad);

        _target.PassiveAbilityController.AddPassive(Object.Instantiate(_passiveSO));

        var tt = _target.Transform;
        Vector3 targetOrigin = tt.position;
        await Tween.Position(tt, targetOrigin + Vector3.up * 1f, 0.20f, Ease.OutQuad);
        await Tween.Position(tt, targetOrigin,                   0.15f, Ease.InQuad);

        await Awaitable.NextFrameAsync();
    }

    public void Undo() { }
}
