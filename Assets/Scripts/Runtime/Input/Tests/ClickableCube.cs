using UnityEngine;

public class ClickableCube : MonoBehaviour, IClickable
{
    private MeshRenderer _renderer;

    private void Awake() => _renderer = GetComponent<MeshRenderer>();

    public void OnHoverEnter() => _renderer.material.color = Color.yellow;

    public void OnHoverExit() => _renderer.material.color = Color.white;

    public void OnClick()
    {
        Debug.Log("Hai cliccato il cubo!");
        transform.localScale *= 1.1f; // Lo ingrandiamo un po' per feedback
    }
}
