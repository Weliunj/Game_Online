using Fusion;
using UnityEngine;

public class HitboxPart : MonoBehaviour
{
    public float damageMultiplier = 1f; // Đầu = 2, Thân = 1, Chân = 0.5
    public StatsHandler rootStats; // Kéo thả StatsHandler ở Root vào đây

    void Awake()
    {
        if (rootStats == null)
        {
            rootStats = GetComponentInParent<StatsHandler>();
        }
    }
    public void OnHit(float baseDamage, PlayerRef shooter) 
    {
        float finalDamage = baseDamage * damageMultiplier;
        rootStats.RPC_TakeDamage(finalDamage, shooter); // Truyền thêm người bắn
    }
}