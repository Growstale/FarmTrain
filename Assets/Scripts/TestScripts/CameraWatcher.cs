using UnityEngine;

public class CameraWatcher : MonoBehaviour
{
    private Camera mainCamera;
    private bool isCameraFound = false;

    void Start()
    {
        // ������� ������� ������ ��� ������
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            isCameraFound = true;
            Debug.Log("<color=green>[Watcher]</color> ������ ������� � �������������!");
        }
        else
        {
            Debug.LogError("<color=red>[Watcher]</color> �� ������� ����� ������� �����u (Camera.main) �� ������ �����!");
        }
    }

    void Update()
    {
        if (!isCameraFound) return;

        // ���������, �� ���� �� ������ ����������
        if (mainCamera == null)
        {
            Debug.LogError("<color=red>[Watcher]</color> ������ ������ ��� ��������� (���� null)!");
            isCameraFound = false; // ���������� ��������, ����� �� ������� � ���
            enabled = false; // ��������� ��� ������
            return;
        }

        // ���������, �� ��� �� ������ ������ �������������
        if (!mainCamera.gameObject.activeInHierarchy)
        {
            Debug.LogError("<color=red>[Watcher]</color> ������ ������ ��� �������� (activeInHierarchy = false)!");
            isCameraFound = false;
            enabled = false;
            return;
        }
    }
}