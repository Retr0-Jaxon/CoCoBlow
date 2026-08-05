using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SimpleHUD simpleHud;

    public int CoconutCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (simpleHud == null)
        {
            simpleHud = FindObjectOfType<SimpleHUD>();
        }
    }

    private void Start()
    {
        RefreshHud();
    }

    public void AddCoconut(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        CoconutCount += amount;
        Debug.Log($"椰子提交成功！+{amount}，当前椰子数量：{CoconutCount}", this);
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (simpleHud != null)
        {
            simpleHud.RefreshAll();
        }
    }
}
