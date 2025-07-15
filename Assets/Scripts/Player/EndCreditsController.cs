using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.UI;

public class EndCreditsController : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Компонент RawImage, на котором будет отображаться видео")]
    [SerializeField] private RawImage videoRawImage;

    [Tooltip("Видео плеер для фона")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Аудио")]
    [Tooltip("Музыкальный трек, который будет играть во время титров")]
    [SerializeField] private AudioClip creditsMusic;

    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private float musicFadeOutTime = 1.5f;

    // Флаг, чтобы предотвратить двойной вызов выхода из сцены
    private bool isExiting = false;

    void Start()
    {
        // --- 1. ПРОВЕРКА КОМПОНЕНТОВ ---
        if (videoPlayer == null || videoRawImage == null)
        {
            Debug.LogError("Не все компоненты назначены в EndCreditsController! Проверьте 'Video Raw Image' и 'Video Player'.");
            enabled = false; // Отключаем скрипт, чтобы избежать ошибок
            return;
        }

        // --- 2. НАСТРОЙКА ВИДЕО И МУЗЫКИ ---
        videoPlayer.isLooping = false;

        if (musicAudioSource == null) musicAudioSource = gameObject.AddComponent<AudioSource>();
        if (creditsMusic != null)
        {
            musicAudioSource.clip = creditsMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }

        videoPlayer.Play();

        // --- 3. ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ ОКОНЧАНИЯ ВИДЕО ---
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        // Позволяем игроку пропустить сцену в любой момент
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }
    }

    /// <summary>
    /// Этот метод вызывается автоматически, когда видео заканчивается.
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("Видео завершено. Выход из игры.");
        ExitGame();
    }

    /// <summary>
    /// Инициирует выход из игры.
    /// </summary>
    public void ExitGame()
    {
        if (isExiting) return;
        isExiting = true;

        videoPlayer.loopPointReached -= OnVideoFinished;

        StartCoroutine(FadeOutAndQuit());
    }

    /// <summary>
    /// Корутина для плавного затухания музыки и полного выхода из игры.
    /// </summary>
    private IEnumerator FadeOutAndQuit()
    {
        // Плавно глушим музыку
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

        // <<< ГЛАВНОЕ ИЗМЕНЕНИЕ ЗДЕСЬ >>>
        Debug.Log("Выход из приложения...");

        // Эта конструкция корректно работает и в редакторе, и в готовой игре.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}