using UnityEngine;

public class TestBackGround : MonoBehaviour
{
    public GameObject BackGrounds;


    public float startX;
    public float startY;

    void Start()
    {
        
    }

    // Update is called once per frame
    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    Debug.Log(collision.gameObject.name);
    //}
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Vector3 position = new Vector3 (-38.97891f, startY, 0); // x: 
        GameObject go = Instantiate(BackGrounds,position,Quaternion.identity); // создаем объект в позиции начала предыдущего
        BackGrounds = go;
       // Debug.Log(startX - BackGrounds.GetComponent<BoxCollider2D>().bounds.min.x);
    }
}
