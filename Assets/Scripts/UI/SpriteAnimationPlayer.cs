using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpriteAnimationPlayer : MonoBehaviour
{
    public Sprite[] sprites;

    private Image image;
    private Coroutine playCoroutine;
    private float frameDuration;
    private Sprite[] frames;
    private bool isLoop;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void PlayAnimation(Sprite[] frames, int fps, bool loop)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("No frames provided for animation.");
            return;
        }
        StopAnimation();
        this.frames = frames;
        frameDuration = 1f / fps;
        isLoop = loop;
        playCoroutine = StartCoroutine(PlayFrame());
    }

    public void StopAnimation()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
    }

    public bool IsPlaying => playCoroutine != null;

    private IEnumerator PlayFrame()
    {
        int frameIndex = 0;
        do
        {
            image.sprite = frames[frameIndex];
            yield return new WaitForSecondsRealtime(frameDuration);
            frameIndex++;
            if (frameIndex >= frames.Length)
            {
                if (isLoop)
                {
                    frameIndex = 0;
                }
                else
                {
                    break;
                }
            }
        } while (true);

        playCoroutine = null;
        gameObject.SetActive(false);
    }
}
