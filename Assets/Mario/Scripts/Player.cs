using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{

    // 
    public CapsuleCollider2D capsuleCollider { get; private set; }
    public DeathAnimation deathAnimation { get; private set; }

    // mario event states
    public bool big => bigRenderer.enabled;
    public bool dead => deathAnimation.enabled;
    public bool starpower { get; private set; }

    // mario renderer objects
    public mario_sprite_renderer smallRenderer;
    public mario_sprite_renderer bigRenderer;
    private mario_sprite_renderer activeRenderer;


    //audio
    public AudioSource starPowerAudio;
    public AudioSource mainThemeAudio;
    public AudioSource growingAudio;
    

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        deathAnimation = GetComponent<DeathAnimation>();
        activeRenderer = smallRenderer;
    }

    public void Hit()
    {
        if (!dead && !starpower)
        {
            if (big) {
                Shrink();
            } else {
                Death();
            }
        }
    }

    public void Death()
    {
        smallRenderer.enabled = false;
        bigRenderer.enabled = false;
        deathAnimation.enabled = true;

        GameManager.Instance.ResetLevel(3f);
    }

    public void Grow()
    {
        
        smallRenderer.enabled = false;
        bigRenderer.enabled = true;
        activeRenderer = bigRenderer;


        capsuleCollider.size = new Vector2(1f, 2f);
        capsuleCollider.offset = new Vector2(0f, 0.5f);

        StartCoroutine(ScaleAnimation());

        growingAudio.Play();
     
    }

    public void Shrink()
    {
        smallRenderer.enabled = true;
        bigRenderer.enabled = false;
        activeRenderer = smallRenderer;

        capsuleCollider.size = new Vector2(1f, 1f);
        capsuleCollider.offset = new Vector2(0f, 0f);

        StartCoroutine(ScaleAnimation());
    }

    private IEnumerator ScaleAnimation()
    {
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (Time.frameCount % 4 == 0)
            {
                smallRenderer.enabled = !smallRenderer.enabled;
                bigRenderer.enabled = !smallRenderer.enabled;
            }

            yield return null;
        }

        smallRenderer.enabled = false;
        bigRenderer.enabled = false;
        activeRenderer.enabled = true;
    }

    public void Starpower()
    {
        // Coroutines are a way to execute code over multiple frames in a controlled manner. 
        //They are often used for animations, delays, or other time-based operations.
    
        StartCoroutine(StarpowerAnimation());
        
    }

    private IEnumerator StarpowerAnimation()
    {
        starpower = true;
        starPowerAudio.Play();
        mainThemeAudio.Stop();
        float elapsed = 0f;
        float duration = 15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (Time.frameCount % 4 == 0) {
                // ranges for color
                float hueMin = 0.5f; // Adjust the hue range as needed
                float hueMax = 0.75f;
                float saturation_min = 0.8f;
                float saturation = 1f;
                float value = 1f;

               // Color randomColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
                Color randomColor = Random.ColorHSV(hueMin, hueMax, saturation_min, saturation, value, value);
                float brightnessFactor = 5.5f;
                randomColor = new Color(
                randomColor.r * brightnessFactor,
                randomColor.g * brightnessFactor,
                randomColor.b * brightnessFactor,
                randomColor.a
                );
                activeRenderer.spriteRenderer.color = randomColor;
                
            }

            yield return null;
        }

        activeRenderer.spriteRenderer.color = Color.white;
        starpower = false;
        starPowerAudio.Stop();
        mainThemeAudio.Play();
    }

}
