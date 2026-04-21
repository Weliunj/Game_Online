using Fusion;
using UnityEngine;

public class HitboxPart : MonoBehaviour
{
    public float damageMultiplier = 1f;
    public StatsHandler rootStats; 
    private int lastHitFrame = -1;

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