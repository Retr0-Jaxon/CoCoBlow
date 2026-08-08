using TMPro;
using UnityEngine;

public class SimpleHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI coconutCountText;
    [SerializeField] private TextMeshProUGUI upgradeHintText;
    [SerializeField] private TextMeshProUGUI darkCountdownText;

    [Header("Display")]
    [SerializeField] private string coconutCountFormat = "已收集椰子数量 : {0}";
    [SerializeField] private string upgradeHintDefaultText = "可以升级了！";
    [SerializeField] private string darkCountdownFormat = "黑暗倒计时: {0:0}";

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    private void Start()
    {
        ShowUpgradeHint(false);
        ShowDarkCountdown(false);
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
}
