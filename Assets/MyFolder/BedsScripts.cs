using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BedsScripts : MonoBehaviour
{
    static List<GameObject> beds = new List<GameObject>();
    
    void Start()
    {
       
    }

    public static void AddBed(GameObject newbed)
    {
        Debug.Log(newbed.name);

        beds.Add(newbed);

    }

    public void CheckFreeSlots()
    {
        BedScript script;
        foreach (var bed in beds) {


            if (bed != null) {
                script = bed.GetComponent<BedScript>();
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
        BedScript script;
        foreach (var bed in beds)
        {


            if (bed != null)
            {
                script = bed.GetComponent<BedScript>();
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
    public void PlantSeed()
    {
        GameObject firstSeed = beds.FirstOrDefault(s=> s.GetComponent<BedScript>().isPlanted == false);
        if (firstSeed != null)
        {
            firstSeed.GetComponent<BedScript>().PlantSeeds();
        }
    }
}
