using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coconut : MonoBehaviour
{
    [SerializeField] private int scoreValue = 1;
    public int ScoreValue => scoreValue;
    public bool IsSubmitted { get; private set; }
    public bool IsDropped { get; private set; }
    private Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing on the Coconut object.", this);
        }
        Freeze();
    }
    public void MarkSubmitted()
    {
        IsSubmitted = true;
    }

    public void Freeze()
    {
        IsDropped = false;
        rb.isKinematic = true;
        rb.useGravity = false;
    }
    public void Unfreeze()
    {
        IsDropped = true;
        rb.isKinematic = false;
        rb.useGravity = true;
    }


}
