using System.Collections.Generic;
using UnityEngine;

public class SlotManagerScript : MonoBehaviour
{
    public Dictionary<GameObject, GameObject[]> bedsDictionary = new Dictionary<GameObject, GameObject[]>();

    public void AddBed(GameObject slot, GameObject[] newbed)
    {

        bedsDictionary.Add(slot, newbed);

    }
    public void ViewList()
    {
        var arrayObjects = bedsDictionary.Values;
        foreach (var beds in arrayObjects)
        {
            foreach (var item in beds)
            {

                Debug.Log(">> " + item.name);
            }
        }
    }

    public void CheckFreeSlots()
    {
        SlotScripts script;

        foreach (var beds in bedsDictionary.Values)
        {


            if (beds != null)
            {
                foreach (var item in beds)
                {

                    script = item.GetComponent<SlotScripts>();
                    if (script && !script.isPlanted)
                    {
                        script.ChangeColor();

                    }
                    else
                    {
                        Debug.Log("No element");
                    }

                }

            }

        }
    }
    public void UnCheckFreeSlots()
    {
        SlotScripts script;

        foreach (var beds in bedsDictionary.Values)
        {


            if (beds != null)
            {
                foreach (var item in beds)
                {

                    script = item.GetComponent<SlotScripts>();
                    if (script && !script.isPlanted)
                    {
                        script.UnChangeColor();

                    }
                    else
                    {
                        Debug.Log("No element");
                    }

                }

            }

        }
    }
}
