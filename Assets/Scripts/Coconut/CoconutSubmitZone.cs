using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoconutSubmitZone : MonoBehaviour
{
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

    private void OnTriggerEnter(Collider other)
    {
        Coconut coconut = other.GetComponentInParent<Coconut>();
        if (coconut == null || coconut.IsSubmitted || gameManager == null)
        {
            return;
        }

        coconut.MarkSubmitted();
        gameManager.AddCoconut(coconut.ScoreValue);
        Destroy(coconut.gameObject);
        //play sfx
        AudioManager.PlayAudio3D("watered", this.gameObject);
    }
}

