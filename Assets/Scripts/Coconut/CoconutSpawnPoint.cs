using UnityEngine;

public class CoconutSpawnPoint : MonoBehaviour
{
    [SerializeField] private Coconut occupyingCoconut;

    public Coconut OccupyingCoconut => occupyingCoconut;
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
}
