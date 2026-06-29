using UnityEngine;

public abstract class AnimationStarter : MonoBehaviour
{
    public AnimationStarter afterThisAnimation;
    public AudioSource hitSound;
    protected abstract void StartAnimation();

    public void StartAnimate()
    {
        if (hitSound != null) hitSound.Play();
        StartAnimation();
    }

    protected void ContinueAnimation()
    {
        if (afterThisAnimation != null) afterThisAnimation.StartAnimate();
    }
}