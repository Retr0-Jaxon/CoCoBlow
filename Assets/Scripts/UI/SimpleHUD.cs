using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI coconutCountText;
    [SerializeField] private TextMeshProUGUI upgradeHintText;
    [SerializeField] private TextMeshProUGUI darkCountdownText;
    [SerializeField] private Image darkVisionOverlay;

    [Header("Display")]
    [SerializeField] private string coconutCountFormat = "已收集椰子数量 : {0}";
    [SerializeField] private string upgradeHintDefaultText = "可以升级了！";
    [SerializeField] private string darkCountdownFormat = "黑暗倒计时: {0:0}";
    [SerializeField] private float darkVisionMinAlpha = 0.7f;
    [SerializeField] private float darkVisionMaxAlpha = 1f;

    private Sprite solidOverlaySprite;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        EnsureSolidOverlaySprite();
    }

    private void Start()
    {
        ShowUpgradeHint(false);
        ShowDarkCountdown(false);
        ShowDarkVision(false, 0f, 1f);
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshCoconutCount();
        RefreshUpgradeHint();
    }

    public void RefreshCoconutCount()
    {
        if (coconutCountText == null || gameManager == null)
        {
            return;
        }

        coconutCountText.text = string.Format(coconutCountFormat, gameManager.CoconutCount);
    }

    public void RefreshUpgradeHint()
    {
        if (upgradeHintText == null)
        {
            return;
        }

        upgradeHintText.text = upgradeHintDefaultText;
    }

    public void ShowUpgradeHint(bool visible)
    {
        if (upgradeHintText != null)
        {
            upgradeHintText.gameObject.SetActive(visible);
        }
    }

    public void RefreshDarkCountdown(float remainingSeconds)
    {
        if (darkCountdownText == null)
        {
            return;
        }

        darkCountdownText.text = string.Format(darkCountdownFormat, remainingSeconds);
    }

    public void ShowDarkCountdown(bool visible)
    {
        if (darkCountdownText != null)
        {
            darkCountdownText.gameObject.SetActive(visible);
        }
    }

    public void ShowDarkVision(bool visible, float remainingSeconds, float totalSeconds)
    {
        if (darkVisionOverlay == null)
        {
            return;
        }

        darkVisionOverlay.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        float t = 1f - Mathf.Clamp01(remainingSeconds / Mathf.Max(0.01f, totalSeconds));
        float alpha = Mathf.Lerp(darkVisionMinAlpha, darkVisionMaxAlpha, t);
        darkVisionOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    private void EnsureSolidOverlaySprite()
    {
        if (darkVisionOverlay == null)
        {
            return;
        }

        solidOverlaySprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        darkVisionOverlay.sprite = solidOverlaySprite;
        darkVisionOverlay.type = Image.Type.Simple;
        darkVisionOverlay.color = new Color(0f, 0f, 0f, darkVisionMinAlpha);
    }

    private void OnDestroy()
    {
        if (solidOverlaySprite != null)
        {
            Destroy(solidOverlaySprite);
            solidOverlaySprite = null;
        }
    }
}
