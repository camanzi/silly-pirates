using System;
using UnityEngine;

[Serializable]
public class AnimationData
{
    public string animationName;
    public int frameCount;
    public float frameRate = 12f;
    public bool loop = true;
}
