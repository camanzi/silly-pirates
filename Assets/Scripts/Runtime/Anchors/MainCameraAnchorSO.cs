using UnityEngine;

/// <summary>
/// Sostituisce <c>Camera.main</c>. I consumatori lo cachavano una volta sola in Awake, il che regge
/// solo finché esiste un unico oggetto taggato MainCamera: con un menu persistente più una scena di
/// combattimento additiva, <c>Camera.main</c> diventa ambiguo e il valore cachato può riferirsi a
/// una camera distrutta.
/// </summary>
[CreateAssetMenu(fileName = "Main Camera Anchor", menuName = "Anchors/Main Camera Anchor")]
public class MainCameraAnchorSO : RuntimeAnchorSO<Camera> { }
