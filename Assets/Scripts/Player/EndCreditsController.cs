using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.UI;

public class EndCreditsController : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Объект с текстом титров (TextMeshPro)")]
    [SerializeField] private GameObject creditsTextObject;

    [Tooltip("Компонент RawImage, на котором будет отображаться видео")]
    [SerializeField] private RawImage videoRawImage;

    [Tooltip("Видео плеер для фона")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Настройки")]
    [Tooltip("Скорость, с которой текст будет двигаться вверх")]
    [SerializeField] private float scrollSpeed = 60f;

    [Tooltip("Длительность финальной сцены в секундах, после чего игра закроется")]
    [SerializeField] private float sceneDuration = 30f;

    [Header("Аудио")]
    [Tooltip("Музыкальный трек, который будет играть во время титров")]
    [SerializeField] private AudioClip creditsMusic;

    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private float musicFadeOutTime = 1.5f;

    // --- Приватные переменные ---
    private RectTransform creditsTextTransform;
    private RectTransform canvasRectTransform;
    private bool isExiting = false;
    private bool isInitialized = false;

    void Start()
    {
        if (RadioManager.Instance != null)
        {
            // Если он существует и его музыка играет, останавливаем ее.
            if (RadioManager.Instance.audioSource != null && RadioManager.Instance.audioSource.isPlaying)
            {
                RadioManager.Instance.audioSource.Stop();
                Debug.Log("Музыка из RadioManager была остановлена контроллером титров.");
            }
        }

        // --- 1. ПРОВЕРКА КОМПОНЕНТОВ ---
        if (videoPlayer == null || videoRawImage == null || creditsTextObject == null)
        {
            Debug.LogError("Не все компоненты назначены! Проверьте 'Credits Text Object', 'Video Raw Image' и 'Video Player'.");
            enabled = false;
            return;
        }

        // --- 2. НАСТРОЙКА И ЗАПУСК ---
        videoPlayer.isLooping = true;

        if (musicAudioSource == null) musicAudioSource = gameObject.AddComponent<AudioSource>();
        if (creditsMusic != null)
        {
            musicAudioSource.clip = creditsMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }

        videoPlayer.Play();

        // Настраиваем начальную позицию текста
        InitializeText();

        // Запускаем таймер, который по истечении времени закроет игру
        StartCoroutine(SceneTimerCoroutine());

        isInitialized = true;
    }

    // Вспомогательный метод для настройки текста
    private void InitializeText()
    {
        creditsTextTransform = creditsTextObject.GetComponent<RectTransform>();
        Canvas parentCanvas = creditsTextObject.GetComponentInParent<Canvas>();
        canvasRectTransform = parentCanvas.GetComponent<RectTransform>();

        // Ставим текст за нижнюю границу экрана
        float startY = -canvasRectTransform.rect.height / 2 - creditsTextTransform.rect.height / 2;
        creditsTextTransform.anchoredPosition = new Vector2(0, startY);
    }


    void Update()
    {
        // Если сцена не инициализирована или уже выходит, ничего не делаем
        if (!isInitialized || isExiting) return;

        // Позволяем игроку пропустить сцену
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
            return; // Выходим из Update, чтобы не двигать текст в том же кадре
        }

        // Двигаем текст вверх каждый кадр
        creditsTextTransform.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
    }

    // Корутина, которая работает как таймер для всей сцены
    private IEnumerator SceneTimerCoroutine()
    {
        yield return new WaitForSeconds(sceneDuration);

        Debug.Log("Таймер сцены истек. Выход из игры.");
        ExitGame();
    }

    /// <summary>
    /// Инициирует выход из игры.
    /// </summary>
    public void ExitGame()
    {
        if (isExiting) return;
        isExiting = true;

        StartCoroutine(FadeOutAndQuit());
    }

    /// <summary>
    /// Корутина для плавного затухания музыки и полного выхода из игры.
    /// </summary>
    private IEnumerator FadeOutAndQuit()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            float startVolume = musicAudioSource.volume;
            float timer = 0f;
            while (timer < musicFadeOutTime)
            {
                timer += Time.deltaTime;
                musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / musicFadeOutTime);
                yield return null;
            }
            musicAudioSource.Stop();
        }

        Debug.Log("Выход из приложения...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}