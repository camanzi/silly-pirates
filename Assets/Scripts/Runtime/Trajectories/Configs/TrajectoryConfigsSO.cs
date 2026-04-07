using UnityEngine;

[CreateAssetMenu(fileName = "Trajectory Config Data", menuName ="Trajectories/Trajectory Config")]
public class TrajectoryConfigsSO : ScriptableObject
{
    [Header("Base Configs")]
    [SerializeField] private float _height;
    [SerializeField] private float _travelDuration;

    public float Height => _height;
    public float TravelDuration => _travelDuration;
}
