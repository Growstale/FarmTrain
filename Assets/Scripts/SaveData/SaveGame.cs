using UnityEngine;

public class SaveGame : MonoBehaviour
{
    public void OnSaveButtonClick()
    {
        if (SaveLoadManager.Instance != null)
        {
            Debug.Log("���������� ���� �� ������� ������...");
            SaveLoadManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogError("��������� SaveLoadManager �� ������!");
        }
    }
}