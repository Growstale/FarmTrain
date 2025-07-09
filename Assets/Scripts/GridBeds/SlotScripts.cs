using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotScripts : MonoBehaviour
{
    public bool isPlanted = false; // åñòü ëè ðàñòåíèå
    public bool ishavebed = false;  // åñòü ëè ãðÿäêà
    public bool isRaked = false; // îáðàáîòàíà ëè ãðÿäêà
    public bool isFertilize = false; // åñòü ëè óäîáðåíèÿ
    Color currentColor;
    SpriteRenderer spriteRenderer;
    Transform slot;



    private InventoryManager inventoryManager; // Ññûëêà íà ìåíåäæåð èíâåíòàðÿ

   

    [SerializeField] ItemSpawner _itemSpawner;
    [SerializeField] Vector3 _sizeBed;
    [SerializeField] Vector3 _sizePlant;

    void Start()
    {

        inventoryManager = InventoryManager.Instance; // È ïîèñê ñèíãëòîíà òîæå
       
        if(_itemSpawner == null)
        {
            Debug.Log("itemSpaner not found!");
        }

       
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentColor = spriteRenderer.color;
       
      
    }

    public void PlantSeeds()
    {

       
        // Ïîëó÷àåì ÂÛÁÐÀÍÍÛÉ ïðåäìåò è ÈÍÄÅÊÑ âûáðàííîãî ñëîòà
        InventoryItem selectedItem = inventoryManager.GetSelectedItem();
        int selectedIndex = inventoryManager.SelectedSlotIndex; // Èñïîëüçóåì íîâîå ñâîéñòâî

       
        if(selectedItem == null)
        {
            Debug.Log("Âûáåðè ïðåäìåò èç èíâåíòîðÿ");
        }

        else
        {
            // Ïðîâåðÿåì, åñòü ëè âûáðàííûé ïðåäìåò è ÿâëÿåòñÿ ëè îí ãîðøêîì èëè ãðÿäêîé
            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Pot)
            {

               

                if (ishavebed)
                {

                    Debug.Log("Òóò óæå çàíÿòî, êóäà??");
                }
                else
                {
                    _itemSpawner.TestSpawnBed(selectedItem.itemData, transform.position, _sizeBed, gameObject.transform);
                    SFXManager.Instance.PlaySFX(SFXManager.Instance.placeBed);
                    ishavebed = true;
                    InventoryManager.Instance.RemoveItem(selectedIndex);

                }

            }

            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Seed && !isPlanted)
            {
                if (ishavebed) {
                    if (isRaked) {
                        if (!isPlanted) {
                            Transform parentSlot = transform.parent;

                            BedSlotController bedSlotController = parentSlot.GetComponent<BedSlotController>();
                            GridGenerator gridGenerator = parentSlot.GetComponent<GridGenerator>();



                            if (parentSlot != null)
                            {
                                float weightSeed = selectedItem.itemData.associatedPlantData.Weight;

                                // ïðîâåðêà âåñà ðàñòåíèÿ 

                                switch (weightSeed)
                                {
                                    case 1:

                                        var tourple1 = gridGenerator.CheckFreeSlot(name);
                                        bool isFreeSlot1 = tourple1.Item1;
                                        Vector3 pos1 = tourple1.Item2;
                                        Vector2Int[] idSlots1 = tourple1.Item3;
                                        if (isFreeSlot1)
                                        {
                                            _itemSpawner.SpawnPlant(selectedItem.itemData, pos1, _sizePlant, gameObject.transform.parent, idSlots1);
                                            AudioClip plantSound = selectedItem.itemData.associatedPlantData.plantingSound;
                                            SFXManager.Instance.PlaySFX(plantSound);

                                            isPlanted = true;
                                            InventoryManager.Instance.RemoveItem(selectedIndex);
                                        }
                                        
                                        break;

                                    case 2:

                                        if (gridGenerator != null)
                                        {
                                            var tourple = gridGenerator.CheckFree2Slot(name);
                                            bool isFreeSlot = tourple.Item1;
                                            Vector3 pos = tourple.Item2;
                                            Vector2Int[] idSlots = tourple.Item3;
                                            if (isFreeSlot)
                                            {
                                                _itemSpawner.SpawnPlant(selectedItem.itemData, pos, _sizePlant, gameObject.transform.parent,idSlots);
                                                AudioClip plantSound = selectedItem.itemData.associatedPlantData.plantingSound;
                                                SFXManager.Instance.PlaySFX(plantSound);
                                                
                                                InventoryManager.Instance.RemoveItem(selectedIndex);
                                            }
                                            else
                                            {
                                                Debug.Log("Íå õâàòàåò ãðÿäîê, íàäî êóïèòü åùå");

                                            }

                                        }
                                        else
                                        {
                                            Debug.Log("Îøèáêà çàïîëíåíèÿ, îòñóòñòâóåò ñêðèïò gridGenerator ó ðîäèòåëüñêîãî Slot");
                                        }
                                        break;
                                    case 4:

                                        if (gridGenerator != null)
                                        {
                                            var tourple = gridGenerator.CheckSquareCells(name);
                                            bool isFreeSlot = tourple.Item1;
                                            Vector3 Plantposition = tourple.Item2;
                                            Vector2Int[] idSlots = tourple.Item3;
                                            if (isFreeSlot)
                                            {
                                                _itemSpawner.SpawnPlant(selectedItem.itemData, Plantposition, _sizePlant, gameObject.transform.parent, idSlots);
                                                AudioClip plantSound = selectedItem.itemData.associatedPlantData.plantingSound;
                                                SFXManager.Instance.PlaySFX(plantSound);

                                                InventoryManager.Instance.RemoveItem(selectedIndex);
                                            }
                                            else
                                            {
                                                Debug.Log("Íå õâàòàåò ãðÿäîê, íàäî êóïèòü åùå");

                                            }

                                        }
                                        else
                                        {
                                            Debug.Log("Îøèáêà çàïîëíåíèÿ, îòñóòñòâóåò ñêðèïò BedSlotController ó ðîäèòåëüñêîãî Slot");
                                        }
                                        break;

                                }

                            }
                            else
                            {
                                Debug.LogError("Íå íàéäåí ðîäèòåëüñêèé ñëîò! Îøèáêà");
                            }
                        }
                        else
                        {
                            Debug.Log("Òóò óæå çàíÿòî, êóäà??");
                        }
                    }
                    else
                    {
                        Debug.Log("Ñíà÷àëà íàäî îáðàáîòàòü ãðÿäêó, à ïîòîì óæå ñàäèòü ðàñòåíèå");

                    }

                }
                else
                {
                    Debug.Log("Ñíà÷àëà íàäî ïîñòàâèòü ãðÿäêó!");
                }

                
               
            }
         

            if(!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Tool)
            {
                if (ishavebed)
                {
                    if (selectedItem.itemData.itemName == "Rake")
                    {
                        GameObject childBed = FindChildWithTag("Bed");
                        if (childBed != null)
                        {
                            BedController bedController = childBed.GetComponent<BedController>();
                            if (bedController != null) {

                                bedController.ChangeStage(BedData.StageGrowthPlant.Raked, 1);
                                isRaked = true;
                                SFXManager.Instance.PlaySFX(SFXManager.Instance.rake);

                            }
                            else
                            {
                                Debug.LogError("bedController íå íàéäåí");
                            }
                        }
                        else
                        {
                            Debug.LogError("Ãðÿäêà íå ÿâëÿåòñÿ äî÷åðíåé äëÿ ñëîòà, îøèáêà");
                        }
                    }
                   

                }
                else
                {
                    Debug.Log("Îáðàáàòûâàòü ìîæíî òîëüêî ïîñàæåííûå ãðÿäêè!");
                }
            }

            //if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Fertilizer)
            //{
            //    if (ishavebed)
            //    {

            //        if (isRaked)
            //        {
            //            if (!isFertilize)
            //            {
            //                
            //                }
            //                else
            //                {
            //                    Debug.LogError("Ãðÿäêà íå ÿâëÿåòñÿ äî÷åðíåé äëÿ ñëîòà, îøèáêà");
            //                }
            //            }
            //            else
            //            {
            //                Debug.Log("Íà ãðÿäêå óæå èìååòñÿ óäîáðåíèå!");
            //            }

            //        }
            //        else
            //        {
            //            Debug.Log("Îáðàáàòûâàòü ìîæíî òîëüêî âñïàõàííûå ãðÿäêè!");
            //        }


            //    }
            //    else
            //    {
            //        Debug.Log("Îáðàáàòûâàòü ìîæíî òîëüêî ïîñàæåííûå ãðÿäêè!");
            //    }
            //}
        }
        
    }

    private GameObject FindChildWithTag(string tag)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(tag))
            {
                return child.gameObject;
            }
        }
        Debug.LogWarning($"No child with tag {tag} found.");
        return null;
    }


    public bool ChangeStateBed(BedData.StageGrowthPlant stage ,int idx)
    {
        GameObject childBed = FindChildWithTag("Bed");
        if (childBed != null)
        {
            BedController bedController = childBed.GetComponent<BedController>();
            if (bedController != null)
            {
                
                bedController.ChangeStage(stage, idx);
                return true;
            }
            else
            {
                Debug.LogError("bedController íå íàéäåí");
                return false;
            }
        }
        return false;
    }

    public void ChangeColor()
    {

        slot.GetComponent<SpriteRenderer>().color = new Color(0, 255f, 0, 0.1f);

    }
    public void UnChangeColor()
    {
        slot.GetComponent<SpriteRenderer>().color = currentColor;
    }
}
