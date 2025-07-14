using UnityEngine;

public class BedCounterTest : MonoBehaviour
{
    void Update()
    {
        // ��� ������� �� ������� B � ������� ��������� ����������
        if (Input.GetKeyDown(KeyCode.B))
        {
            // �������� ���������� ������ � ���������
            int bedsInInventory = InventoryManager.Instance.GetTotalItemQuantityByType(ItemType.Pot);

            // �������� ���������� ������ � ������
            int bedsOnWagon = PlantManager.instance.GetPlacedBedsCount();

            // ������� ���������
            Debug.Log($"--- ���������� �� ������� ---");
            Debug.Log($"� ���������: {bedsInInventory} ��.");
            Debug.Log($"����������� � ������: {bedsOnWagon} ��.");
            Debug.Log($"-----------------------------");
        }
    }
}