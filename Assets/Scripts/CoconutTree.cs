using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoconutTree : MonoBehaviour
{   
    [SerializeField] private GameObject coconutPrefab;
    [SerializeField] private Transform coconutSocket;

    // Start is called before the first frame update
    void Start()
    {
        if (coconutPrefab == null){
            Debug.LogError("Coconut prefab is not assigned in the inspector.", this);
        }
        // Optionally///////////////////////////////////
        SpawnCoconut();
        ////////////////////////////////////////////////////////////////////////
    }


    void Update()
    {
        
    }

    public void SpawnCoconut()
    {
        if (coconutPrefab != null)
        {
            GameObject newCoconut = Instantiate(coconutPrefab, coconutSocket.position, coconutSocket.rotation);
            newCoconut.GetComponent<Coconut>().Freeze();
            newCoconut.transform.SetParent(coconutSocket, true);
        }
    }

}
