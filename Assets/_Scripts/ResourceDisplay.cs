using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ResourceDisplay : MonoBehaviour
{
    [Header("Stat Barý")]
    public Image fillImage; // <-- BU ALAN EKLENDÝ

    [Header("Kriz Efekti")]
    public Color normalColor = Color.white;
    public Color criticalColor = Color.red;
    public float blinkSpeed = 5f;

    private Coroutine _blinkCoroutine;

    public void SetBackgroundColor(Color color)
    {
        // Arka plan rengini deðiþtir (isteðe baðlý)
        var bg = GetComponent<Image>();
        if (bg != null) bg.color = color;
    }

    public void StartBlinking()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlinking()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        if (fillImage != null) fillImage.color = normalColor;
    }

    private IEnumerator BlinkRoutine()
    {
        float timer = 0;
        while (timer < 2f)
        {
            timer += Time.deltaTime;
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(normalColor, criticalColor,
                    Mathf.PingPong(Time.time * blinkSpeed, 1f));
            }
            yield return null;
        }
        if (fillImage != null) fillImage.color = normalColor;
        _blinkCoroutine = null;
    }

    // Eski UpdateBars metodunu kaldýrdýk – Artýk GameEvents.OnResourcesUpdated ile güncelleniyor.
}