using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class KeepInsideArea : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float countdownSeconds = 7f;
    [SerializeField] private Transform respawnPoint;

    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null && !zoneCollider.isTrigger)
        {
            zoneCollider.isTrigger = true;
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

    private void PlayerInBounds()
    {
        CancelInvoke(nameof(TeleportPlayer));
        AudioManager.StopAudio("out_of_bound");
    }
    private void PlayerOutOfBounds()
    {
        AudioManager.PlayAudio("out_of_bound", false);
        Invoke(nameof(TeleportPlayer), countdownSeconds);
    }

    private void TeleportPlayer()
    {
        if (player == null || respawnPoint == null)
            return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        player.transform.position = respawnPoint.position;

        if (cc != null)
            cc.enabled = true;
    }
}
