using System.Collections.Generic;
using UnityEngine;

public class BedsManagerScript : MonoBehaviour
{
    public List<GameObject> beds = new List<GameObject>();
    void Start()
    {
        GameObject sq = GameObject.CreatePrimitive(PrimitiveType.Cube);
    }

    public void AddBed(GameObject newbed)
    {
        Debug.Log(newbed.name);

        beds.Add(newbed);

    }
    public void ViewList()
    {
        foreach (GameObject bed in beds)
        {
            Debug.Log("<<<" + bed.name);
        }
    }

    public void CheckFreeSlots()
    {
        BedsScripts script;
        foreach (var bed in beds)
        {


            if (bed != null)
            {
                script = bed.GetComponent<BedsScripts>();
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
    public void UnCheckFreeSlots()
    {
        BedsScripts script;
        foreach (var bed in beds)
        {


            if (bed != null)
            {
                script = bed.GetComponent<BedsScripts>();
                if (script)
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
