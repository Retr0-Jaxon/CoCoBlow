using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DarkZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleHUD simpleHud;
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;

    [Header("Settings")]
    [SerializeField] private float countdownSeconds = 10f;

    private Collider zoneCollider;
    private CharacterController playerController;
    private float remainingTime;
    private bool isPlayerInSafeZone = true;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        if (simpleHud == null)
        {
            simpleHud = FindObjectOfType<SimpleHUD>();
        }

        if (player == null)
        {
            FirstPersonController controller = FindObjectOfType<FirstPersonController>();
            if (controller != null)
            {
                player = controller.transform;
            }
        }

        CachePlayerController();
    }

    private void Start()
    {
        remainingTime = countdownSeconds;
        isPlayerInSafeZone = IsPlayerInsideSafeZone();
        UpdateHud(!isPlayerInSafeZone);
    }

    private void Update()
    {
        bool insideSafeZone = IsPlayerInsideSafeZone();
        if (insideSafeZone != isPlayerInSafeZone)
        {
            isPlayerInSafeZone = insideSafeZone;
            if (isPlayerInSafeZone)
            {
                EnterSafeZone();
            }
            else
            {
                LeaveSafeZone();
            }
        }

        if (isPlayerInSafeZone)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (simpleHud != null)
        {
            simpleHud.RefreshDarkCountdown(Mathf.Max(0f, remainingTime));
        }

        if (remainingTime <= 0f)
        {
            RespawnPlayer();
        }
    }

    private void CachePlayerController()
    {
        playerController = player != null ? player.GetComponent<CharacterController>() : null;
    }

    private bool IsPlayerInsideSafeZone()
    {
        if (zoneCollider == null || player == null)
        {
            return true;
        }

        if (playerController == null)
        {
            CachePlayerController();
        }

        if (playerController == null)
        {
            return true;
        }

        return zoneCollider.bounds.Intersects(GetPlayerBounds());
    }

    private Bounds GetPlayerBounds()
    {
        Vector3 center = player.TransformPoint(playerController.center);
        float diameter = playerController.radius * 2f;
        return new Bounds(center, new Vector3(diameter, playerController.height, diameter));
    }

    private void EnterSafeZone()
    {
        remainingTime = countdownSeconds;
        UpdateHud(false);
    }

    private void LeaveSafeZone()
    {
        remainingTime = countdownSeconds;
        UpdateHud(true);
    }

    private void RespawnPlayer()
    {
        if (player == null || respawnPoint == null)
        {
            remainingTime = countdownSeconds;
            isPlayerInSafeZone = true;
            UpdateHud(false);
            return;
        }

        if (playerController == null)
        {
            CachePlayerController();
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        player.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        isPlayerInSafeZone = IsPlayerInsideSafeZone();
        remainingTime = countdownSeconds;
        UpdateHud(!isPlayerInSafeZone);
    }

    private void UpdateHud(bool showCountdown)
    {
        if (simpleHud == null)
        {
            return;
        }

        simpleHud.ShowDarkCountdown(showCountdown);
        if (showCountdown)
        {
            simpleHud.RefreshDarkCountdown(remainingTime);
        }
    }
}
