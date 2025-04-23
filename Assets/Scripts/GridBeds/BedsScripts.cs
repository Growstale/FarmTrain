using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BedsScripts : MonoBehaviour
{
    public bool isPlanted = false;
    Color currentColor;
    SpriteRenderer spriteRenderer;
    Transform seed;
    void Start()
    {


        spriteRenderer = GetComponent<SpriteRenderer>();

        seed = transform.Find("Seed");
        currentColor = spriteRenderer.color;

        if (seed != null)
        {
            Debug.Log("Found child object: " + seed.name);
        }
        else
        {
            Debug.Log("Child object not found");
        }
    }


    public void PlantSeeds()
    {
        isPlanted = true;
        seed.gameObject.SetActive(true);
    }
    public void ChangeColor()
    {

        GetComponent<SpriteRenderer>().color = new Color(0, 255f, 0, 0.1f);

    }
    public void UnChangeColor()
    {
        GetComponent<SpriteRenderer>().color = currentColor;
    }
}
