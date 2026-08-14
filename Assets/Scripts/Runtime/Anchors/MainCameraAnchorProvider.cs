using UnityEngine;

public class MainCameraAnchorProvider : RuntimeAnchorProvider<MainCameraAnchorSO, Camera>
{
    protected override Camera Resolve() => GetComponent<Camera>();
}
