using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class CutsceneManager : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneFrame
    {
        public Sprite image;
        public string text;
    }

    public CutsceneFrame[] frames;
    public Image displayImage;
    public TextMeshProUGUI displayText;
    public GameObject cutsceneCanvas;
    public Button skipButton;
    private float delayBeforeLoad = 1f;

    private int currentFrameIndex = 0;
    private bool isCutsceneActive = false;

    void Start()
    {
        cutsceneCanvas.SetActive(true);
        ShowFrame(0);
        isCutsceneActive = true;

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCutscene);
        }
    }

    void Update()
    {
        if (isCutsceneActive && Input.GetMouseButtonDown(0))
        {
            ShowNextFrame();
        }
    }

    void ShowFrame(int index)
    {
        if (index < frames.Length)
        {
            displayImage.sprite = frames[index].image;
            displayText.text = frames[index].text;
            currentFrameIndex = index;
        }
        else
        {
            EndCutscene();
        }
    }

    void ShowNextFrame()
    {
        ShowFrame(currentFrameIndex + 1);
    }

    void SkipCutscene()
    {
        EndCutscene();
    }

    void EndCutscene()
    {
        isCutsceneActive = false;
        StartCoroutine(LoadGameSceneAfterDelay());
    }

    IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene("SampleScene");
    }
}