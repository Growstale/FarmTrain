using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

public class BedSlotController : MonoBehaviour
{
    private Dictionary<int, GameObject> bedSlots = new Dictionary<int, GameObject>(); 

    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            bedSlots.Add(i + 1, child);
        }

    }

    public Vector2 Plant4Slot()
    {
        
            foreach (var slot in bedSlots.Values)
            {
                SlotScripts slotScripts = slot.GetComponent<SlotScripts>();
                if (slotScripts != null)
                {
                    slotScripts.isPlanted = true;

                }
                else
                {
                    Debug.LogError("Ошибка, отсутствует скрипт slotScripts у грядки");
                   
                }
            }

            return transform.position;
        
    }



    public Vector2 Plant2Slot(string name)
    {
        GameObject bedSlot1 = bedSlots[1];
        GameObject bedSlot2 = bedSlots[2];
        GameObject bedSlot3 = bedSlots[3];
        GameObject bedSlot4 = bedSlots[4];

        int numberid = 0;
       // извлекаем id слота 
        Match match = Regex.Match(name, @"\d+");

        if (match.Success)
        {
            numberid = int.Parse(match.Value);

           
        }

       

        if (bedSlot1 != null && bedSlot2 != null && bedSlot3 != null && bedSlot4 != null)
        {
            SlotScripts slotScripts1 = bedSlot1.GetComponent<SlotScripts>();
            SlotScripts slotScripts2 = bedSlot2.GetComponent<SlotScripts>();
            SlotScripts slotScripts3 = bedSlot3.GetComponent<SlotScripts>();
            SlotScripts slotScripts4 = bedSlot4.GetComponent<SlotScripts>();

            if (slotScripts1 != null && slotScripts2 != null && slotScripts3 != null && slotScripts4 != null)
            {
                
                if(numberid == 1 || numberid == 2)
                {
                    if ((!slotScripts1.isPlanted && slotScripts1.ishavebed && slotScripts1.isRaked) && (!slotScripts2.isPlanted && slotScripts2.ishavebed && slotScripts2.isRaked))
                    {
                        slotScripts1.isPlanted = true;
                        slotScripts2.isPlanted = true;
                        return (bedSlot1.transform.position + bedSlot2.transform.position) / 2;

                    }
                   

                }

                if (numberid == 3 || numberid == 4)
                {
                    if ((!slotScripts3.isPlanted && slotScripts3.ishavebed && slotScripts3.isRaked) && (!slotScripts4.isPlanted && slotScripts4.ishavebed && slotScripts4.isRaked))
                    {
                        slotScripts3.isPlanted = true;
                        slotScripts4.isPlanted = true;
                        return (bedSlot3.transform.position + bedSlot4.transform.position) / 2;
                    }
                }
                return new Vector2(1, 1);
            }
            else
            {
                Debug.Log("slotScripts не найден у bedSlot ");
                return new Vector2(1, 1);
            }


        }
        else
        {
            Debug.Log("Не найден bedSlot ");
            return new Vector2(1, 1);
        }

    }


    public bool CheckFreeSlot(int count)
    {
        bool isfreeslot = true;
        // проверка всего слота
        if (count == 4)
        {
            foreach (var slot in bedSlots.Values)
            {
                // получаем скрипт для проверки на наличие грядки 
                SlotScripts slotScripts = slot.GetComponent<SlotScripts>();
                if (slotScripts != null)
                {
                    if (slotScripts.isPlanted || !slotScripts.ishavebed || !slotScripts.isRaked) return false; 

                }
                else
                {
                    Debug.LogError("Ошибка, отсутствует скрипт slotScripts у грядки");
                    return false;
                }
            }

        }
        if (count == 2) {

            GameObject bedSlot1 = bedSlots[1];
            GameObject bedSlot2 = bedSlots[2];
            GameObject bedSlot3 = bedSlots[3];
            GameObject bedSlot4 = bedSlots[4];

            if(bedSlot1 != null && bedSlot2 != null && bedSlot3 != null && bedSlot4 != null)
            {
                SlotScripts slotScripts1 = bedSlot1.GetComponent<SlotScripts>();
                SlotScripts slotScripts2 = bedSlot2.GetComponent<SlotScripts>();
                SlotScripts slotScripts3 = bedSlot3.GetComponent<SlotScripts>();
                SlotScripts slotScripts4 = bedSlot4.GetComponent<SlotScripts>();

                if (slotScripts1 != null)
                {
                    if ((!slotScripts1.isPlanted && slotScripts1.ishavebed)&& (!slotScripts2.isPlanted && slotScripts2.ishavebed))
                    {

                        return true;
                    }
                    if ((!slotScripts3.isPlanted && slotScripts3.ishavebed)&& (!slotScripts4.isPlanted && slotScripts4.ishavebed))
                    {

                        return true;
                    }
                    return false;
                }
            }

        }
        return isfreeslot;
    }

}
