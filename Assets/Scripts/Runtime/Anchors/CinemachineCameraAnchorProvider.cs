using Unity.Cinemachine;

public class CinemachineCameraAnchorProvider : RuntimeAnchorProvider<CinemachineCameraAnchorSO, CinemachineCamera>
{
    protected override CinemachineCamera Resolve() => GetComponent<CinemachineCamera>();
}
