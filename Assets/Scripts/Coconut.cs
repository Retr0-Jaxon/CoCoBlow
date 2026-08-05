using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coconut : MonoBehaviour
{
    [SerializeField] private int scoreValue = 1;
    public int ScoreValue => scoreValue;
    public bool IsSubmitted { get; private set; }
    public void MarkSubmitted()
    {
        IsSubmitted = true;
    }
}
