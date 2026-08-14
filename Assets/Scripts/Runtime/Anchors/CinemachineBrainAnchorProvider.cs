using Unity.Cinemachine;

public class CinemachineBrainAnchorProvider : RuntimeAnchorProvider<CinemachineBrainAnchorSO, CinemachineBrain>
{
    protected override CinemachineBrain Resolve() => GetComponent<CinemachineBrain>();
}
