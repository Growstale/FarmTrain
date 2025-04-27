using System.Collections.Generic;
using UnityEngine;

public class BedsManagerScript : MonoBehaviour
{
    public Dictionary<GameObject, GameObject[]> bedsDictionary = new Dictionary<GameObject, GameObject[]>();




    public void AddBed(GameObject slot,GameObject[] newbed)
    {

        bedsDictionary.Add(slot,newbed);

    }
    public void ViewList()
    {
        var arrayObjects = bedsDictionary.Values;
        foreach (var beds in arrayObjects)
        {
            foreach (var item in beds) { 
            
                Debug.Log( ">> " + item.name);
            }
        }
    }

    public void CheckFreeSlots()
    {
        BedsScripts script;

        foreach (var beds in bedsDictionary.Values)
        {


            if (beds != null)
            {
                foreach (var item in beds) {

                    script = item.GetComponent<BedsScripts>();
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
        BedsScripts script;

        foreach (var beds in bedsDictionary.Values)
        {


            if (beds != null)
            {
                foreach (var item in beds)
                {

                    script = item.GetComponent<BedsScripts>();
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
