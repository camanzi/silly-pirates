using System;

[Serializable]
public class AnimationConfig
{
    public EAnimation animation;
    public int frameCount;
    public float frameRate = 12f;
    public bool loop = true;
}
