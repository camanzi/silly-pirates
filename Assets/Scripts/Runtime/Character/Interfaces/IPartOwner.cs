public interface IPartOwner
{
    bool IsPartFunctional(EnemyPartSO part);
    UnityEngine.Transform GetPartTransform(EnemyPartSO part);
}
