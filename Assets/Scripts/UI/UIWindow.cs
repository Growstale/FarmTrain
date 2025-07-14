using UnityEngine;

public class UIWindow : MonoBehaviour
{
    [SerializeField] private GameObject window;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
    [SerializeField] private bool closeOtherWindows = true;

    private void Awake()
    {
        if (window == null)
            window = gameObject;
    }

    private void Start()
    {
        Close();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWindow();
        }
    }

    public void ToggleWindow()
    {
        if (window.activeSelf)
        {
            Close();
        }
        else
        {
            if (closeOtherWindows)
                CloseAllOtherWindows();
            Open();
        }
    }

    public void Open()
    {
        window.SetActive(true);
    }

    public void Close()
    {
        window.SetActive(false);
    }

    private void CloseAllOtherWindows()
    {
        foreach (UIWindow otherWindow in FindObjectsOfType<UIWindow>())
        {
            if (otherWindow != this && otherWindow.window != null)
                otherWindow.Close();
        }
    }
}