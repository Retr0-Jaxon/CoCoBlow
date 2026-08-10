using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class KeepInsideArea : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameManager gameManager;
    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null && !zoneCollider.isTrigger)
        {
            zoneCollider.isTrigger = true;
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null
                ? GameManager.Instance
                : FindObjectOfType<GameManager>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            PlayerOutOfBounds();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            PlayerInBounds();
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject == player)
        {
            PlayerInBounds();
        }
    }
    private void PlayerInBounds()
    {
        AudioManager.StopAudio("out_of_bound");
    }
    private void PlayerOutOfBounds()
    {
        AudioManager.PlayAudio("out_of_bound", false);
    }
}