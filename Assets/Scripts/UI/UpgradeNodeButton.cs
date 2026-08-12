using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UpgradeNodeButton : MonoBehaviour
{
    [SerializeField] private SimplePanelUI panelUI;
    [SerializeField] private SimplePanelUI.UpgradeCategory category;
    [SerializeField] private int nodeIndex;
    [SerializeField] private UpgradeSubMenuLayout layout;

    private void Awake()
    {
        if (panelUI == null)
        {
            panelUI = FindObjectOfType<SimplePanelUI>();
        }

        Button button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        if (panelUI == null)
        {
            return;
        }

        if (category == SimplePanelUI.UpgradeCategory.HairDryer)
        {
            panelUI.SelectHairDryerUpgradeNode(nodeIndex, layout);
        }
        else
        {
            panelUI.SelectCoconutTreeUpgradeNode(nodeIndex, layout);
        }
    }
}
