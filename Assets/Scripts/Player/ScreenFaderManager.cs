// ScreenFaderManager.cs

using UnityEngine;
using System.Collections;
using UnityEngine.UI; // <<< Добавьте это

public class ScreenFaderManager : MonoBehaviour
{
    public static ScreenFaderManager Instance { get; private set; }

    [SerializeField] private CanvasGroup faderCanvasGroup; // <<< ИЗМЕНЕНИЕ: теперь ссылка на CanvasGroup
    [SerializeField] private float fadeDuration = 1.0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // В инспекторе перетащите ваш FaderImage с компонентом CanvasGroup в это поле
        if (faderCanvasGroup == null)
        {
            Debug.LogError("Fader Canvas Group не назначен в ScreenFaderManager!");
            return;
        }

        // Убеждаемся, что он прозрачен при старте
        faderCanvasGroup.alpha = 0f;
    }

    // ScreenFaderManager.cs
    public IEnumerator FadeOutAndInCoroutine(System.Action onMiddleOfFade = null)
    {
        // Код остается тем же, просто меняется название метода
        yield return StartCoroutine(Fade(1f));
        onMiddleOfFade?.Invoke();
        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator FadeCoroutine(System.Action onMiddleOfFade)
    {
        // 1. Плавное затемнение (Fade Out)
        yield return StartCoroutine(Fade(1f));

        // 2. Действие в середине, пока экран черный
        onMiddleOfFade?.Invoke();

        // 3. Плавное осветление (Fade In)
        yield return StartCoroutine(Fade(0f));
    }

    // Вспомогательная корутина для самого процесса изменения альфы
    private IEnumerator Fade(float targetAlpha)
    {
        float time = 0;
        float startAlpha = faderCanvasGroup.alpha;

        while (time < fadeDuration)
        {
            // Lerp - линейная интерполяция от startAlpha к targetAlpha за время
            faderCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            time += Time.deltaTime;
            yield return null; // Ждем следующего кадра
        }

        // Гарантируем, что в конце альфа будет точно равна целевому значению
        faderCanvasGroup.alpha = targetAlpha;
    }

    public IEnumerator FadeToAlpha(float targetAlpha)
    {
        float time = 0;
        float startAlpha = faderCanvasGroup.alpha;

        while (time < fadeDuration)
        {
            faderCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        faderCanvasGroup.alpha = targetAlpha;
    }

}