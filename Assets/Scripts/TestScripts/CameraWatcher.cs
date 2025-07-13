using UnityEngine;

public class CameraWatcher : MonoBehaviour
{
    private Camera mainCamera;
    private bool isCameraFound = false;

    void Start()
    {
        // Находим главную камеру при старте
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            isCameraFound = true;
            Debug.Log("<color=green>[Watcher]</color> Камера найдена и отслеживается!");
        }
        else
        {
            Debug.LogError("<color=red>[Watcher]</color> Не удалось найти главную камерu (Camera.main) на старте сцены!");
        }
    }

    void Update()
    {
        if (!isCameraFound) return;

        // Проверяем, не была ли камера уничтожена
        if (mainCamera == null)
        {
            Debug.LogError("<color=red>[Watcher]</color> ОБЪЕКТ КАМЕРЫ БЫЛ УНИЧТОЖЕН (стал null)!");
            isCameraFound = false; // Прекращаем проверку, чтобы не спамить в лог
            enabled = false; // Отключаем сам скрипт
            return;
        }

        // Проверяем, не был ли объект камеры деактивирован
        if (!mainCamera.gameObject.activeInHierarchy)
        {
            Debug.LogError("<color=red>[Watcher]</color> ОБЪЕКТ КАМЕРЫ БЫЛ ВЫКЛЮЧЕН (activeInHierarchy = false)!");
            isCameraFound = false;
            enabled = false;
            return;
        }
    }
}