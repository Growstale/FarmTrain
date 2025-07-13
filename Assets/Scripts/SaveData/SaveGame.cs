using UnityEngine;

public class SaveGame : MonoBehaviour
{
    public void OnSaveButtonClick()
    {
        if (SaveLoadManager.Instance != null)
        {
            Debug.Log("—охранение игры по нажатию кнопки...");
            SaveLoadManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogError("Ёкземпл€р SaveLoadManager не найден!");
        }
    }
}