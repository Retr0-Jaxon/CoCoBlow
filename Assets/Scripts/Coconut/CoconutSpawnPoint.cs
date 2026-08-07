using UnityEngine;

public class CoconutSpawnPoint : MonoBehaviour
{
    [SerializeField] private Coconut occupyingCoconut;

    public bool IsOccupied => occupyingCoconut != null;

    public bool TryOccupy(Coconut coconut)
    {
        if (coconut == null || occupyingCoconut != null)
        {
            return false;
        }

        occupyingCoconut = coconut;
        return true;
    }

    public void ClearOccupant()
    {
        occupyingCoconut = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? new Color(1f, 0.5f, 0f, 0.8f) : new Color(0.2f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.18f);
    }
}
