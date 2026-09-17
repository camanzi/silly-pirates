using UnityEngine;

/// <summary>
/// Replaces <c>Camera.main</c>. Consumers used to cache it once in Awake, which only holds while a
/// single object is tagged MainCamera: with a persistent menu plus an additive combat scene,
/// <c>Camera.main</c> becomes ambiguous and the cached value can refer to a destroyed camera.
/// </summary>
[CreateAssetMenu(fileName = "Main Camera Anchor", menuName = "Anchors/Main Camera Anchor")]
public class MainCameraAnchorSO : RuntimeAnchorSO<Camera> { }
