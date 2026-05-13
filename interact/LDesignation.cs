using UnityEngine;
using CreatureTypes;
using static CreatureTypes.CreatureID;

public class LDesignation : MonoBehaviour
{
    [Tooltip("플레이어가 이 L에게 시키고 싶은 합성.")]
    public CreatureID playerDesignation = CreatureID.H;

    [SerializeField] private TentacleGrab tentacleGrab;

    private void Awake()
    {
        if (tentacleGrab == null) tentacleGrab = GetComponent<TentacleGrab>();
        if (tentacleGrab == null) tentacleGrab = GetComponentInChildren<TentacleGrab>();
    }

    private void Update()
    {
        if (tentacleGrab == null) return;
    }
}
