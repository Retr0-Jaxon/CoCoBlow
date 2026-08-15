using UnityEngine;

public class AnomalyController : MonoBehaviour
{
    public static AnomalyController Instance { get; private set; }

    [Header("Anomaly 1 - Land Flash")]
    [SerializeField] private float landFlashDuration = 0.3f;
    [SerializeField] private float landFlashIntensity = 8f;
    [SerializeField] private float landFlashRange = 4f;

    [Header("Anomaly 2 - Burst Toward Player")]
    [SerializeField] private float randomBurstImpulse = 6.5f;

    [Header("Anomaly 3 - Sound")]
    [SerializeField] private string anomalySoundName = "anomaly";

    [Header("Anomaly 4 - Deco Tree Glitch")]
    [SerializeField] private float decoGlitchDuration = 4f;
    [SerializeField] private float decoGlitchMinInterval = 0.03f;
    [SerializeField] private float decoGlitchMaxInterval = 0.12f;
    [SerializeField] private float decoGlitchMaxAngle = 25f;
    [SerializeField] private bool enableAnomalyGlitch = true;

    public bool FlashOnLand { get; private set; }
    public bool RandomBurstOnLand { get; private set; }

    private Transform player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        CachePlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OnHairDryerUpgraded(int level)
    {
        if (level >= 1)
        {
            FlashOnLand = true;
        }

        if (level >= 2)
        {
            RandomBurstOnLand = true;
        }

        if (level >= 3 && !string.IsNullOrEmpty(anomalySoundName))
        {
            AudioManager.PlayAudio(anomalySoundName, false);
        }

        if (level >= 3 && enableAnomalyGlitch)
        {
            foreach (DecoTree tree in FindObjectsOfType<DecoTree>())
            {
                tree.TriggerRotation(
                    decoGlitchDuration,
                    decoGlitchMinInterval,
                    decoGlitchMaxInterval,
                    decoGlitchMaxAngle);
            }
        }
    }

    public void HandleCoconutLanded(Coconut coconut)
    {
        if (coconut == null)
        {
            return;
        }

        if (FlashOnLand)
        {
            coconut.PlayLandFlash(landFlashDuration, landFlashIntensity, landFlashRange);
        }

        if (RandomBurstOnLand)
        {
            if (player == null)
            {
                CachePlayer();
            }

            coconut.ApplyLandBurstToward(randomBurstImpulse, player);
        }
    }

    private void CachePlayer()
    {
        FirstPersonController controller = FindObjectOfType<FirstPersonController>();
        if (controller != null)
        {
            player = controller.transform;
        }
    }
}
