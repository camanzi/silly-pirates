using Unity.Cinemachine;

public class CinemachineTargetGroupAnchorProvider : RuntimeAnchorProvider<CinemachineTargetGroupAnchorSO, CinemachineTargetGroup>
{
    protected override CinemachineTargetGroup Resolve() => GetComponent<CinemachineTargetGroup>();
}
