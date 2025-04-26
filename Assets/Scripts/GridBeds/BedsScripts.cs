using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BedsScripts : MonoBehaviour
{
    public bool isPlanted = false;
    Color currentColor;
    SpriteRenderer spriteRenderer;
    Transform slot;

    void Start()
    {


       

        slot = transform.Find("Square");
        spriteRenderer = slot.GetComponent<SpriteRenderer>();
        currentColor = spriteRenderer.color;
        if (slot == null)
        {
            Debug.Log("not find");
        }
        else
        {
            Debug.Log("<< find");
        }
      
    }


    public void PlantSeeds()
    {
        isPlanted = true;
       // seed.gameObject.SetActive(true);
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
