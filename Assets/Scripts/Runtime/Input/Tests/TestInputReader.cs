using UnityEngine;

public class MouseTest : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;

    private void OnEnable() => _inputReader.ClickStartedEvent += TestClick;
    private void OnDisable() => _inputReader.ClickStartedEvent -= TestClick;

    private void TestClick()
    {
        Debug.Log("<color=green>Click successfully detected by the UI map!</color>");
    }
}
