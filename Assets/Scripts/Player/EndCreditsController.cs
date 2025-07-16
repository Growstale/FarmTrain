using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.UI;

public class EndCreditsController : MonoBehaviour
{
    [Header("����������")]
    [Tooltip("������ � ������� ������ (TextMeshPro)")]
    [SerializeField] private GameObject creditsTextObject;

    [Tooltip("��������� RawImage, �� ������� ����� ������������ �����")]
    [SerializeField] private RawImage videoRawImage;

    [Tooltip("����� ����� ��� ����")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("���������")]
    [Tooltip("��������, � ������� ����� ����� ��������� �����")]
    [SerializeField] private float scrollSpeed = 60f;

    [Tooltip("������������ ��������� ����� � ��������, ����� ���� ���� ���������")]
    [SerializeField] private float sceneDuration = 30f;

    [Header("�����")]
    [Tooltip("����������� ����, ������� ����� ������ �� ����� ������")]
    [SerializeField] private AudioClip creditsMusic;

    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private float musicFadeOutTime = 1.5f;

    // --- ��������� ���������� ---
    private RectTransform creditsTextTransform;
    private RectTransform canvasRectTransform;
    private bool isExiting = false;
    private bool isInitialized = false;

    void Start()
    {
        if (RadioManager.Instance != null)
        {
            // ���� �� ���������� � ��� ������ ������, ������������� ��.
            if (RadioManager.Instance.audioSource != null && RadioManager.Instance.audioSource.isPlaying)
            {
                RadioManager.Instance.audioSource.Stop();
                Debug.Log("������ �� RadioManager ���� ����������� ������������ ������.");
            }
        }

        // --- 1. �������� ����������� ---
        if (videoPlayer == null || videoRawImage == null || creditsTextObject == null)
        {
            Debug.LogError("�� ��� ���������� ���������! ��������� 'Credits Text Object', 'Video Raw Image' � 'Video Player'.");
            enabled = false;
            return;
        }

        // --- 2. ��������� � ������ ---
        videoPlayer.isLooping = true;

        if (musicAudioSource == null) musicAudioSource = gameObject.AddComponent<AudioSource>();
        if (creditsMusic != null)
        {
            musicAudioSource.clip = creditsMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }

        videoPlayer.Play();

        // ����������� ��������� ������� ������
        InitializeText();

        // ��������� ������, ������� �� ��������� ������� ������� ����
        StartCoroutine(SceneTimerCoroutine());

        isInitialized = true;
    }

    // ��������������� ����� ��� ��������� ������
    private void InitializeText()
    {
        creditsTextTransform = creditsTextObject.GetComponent<RectTransform>();
        Canvas parentCanvas = creditsTextObject.GetComponentInParent<Canvas>();
        canvasRectTransform = parentCanvas.GetComponent<RectTransform>();

        // ������ ����� �� ������ ������� ������
        float startY = -canvasRectTransform.rect.height / 2 - creditsTextTransform.rect.height / 2;
        creditsTextTransform.anchoredPosition = new Vector2(0, startY);
    }


    void Update()
    {
        // ���� ����� �� ���������������� ��� ��� �������, ������ �� ������
        if (!isInitialized || isExiting) return;

        // ��������� ������ ���������� �����
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
            return; // ������� �� Update, ����� �� ������� ����� � ��� �� �����
        }

        // ������� ����� ����� ������ ����
        creditsTextTransform.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
    }

    // ��������, ������� �������� ��� ������ ��� ���� �����
    private IEnumerator SceneTimerCoroutine()
    {
        yield return new WaitForSeconds(sceneDuration);

        Debug.Log("������ ����� �����. ����� �� ����.");
        ExitGame();
    }

    /// <summary>
    /// ���������� ����� �� ����.
    /// </summary>
    public void ExitGame()
    {
        if (isExiting) return;
        isExiting = true;

        StartCoroutine(FadeOutAndQuit());
    }

    /// <summary>
    /// �������� ��� �������� ��������� ������ � ������� ������ �� ����.
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

        Debug.Log("����� �� ����������...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}