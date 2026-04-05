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
    public void OnHit(float baseDamage)
    {
        float finalDamage = baseDamage * damageMultiplier;
        Debug.Log($"[HitboxPart] {gameObject.name} - Base DMG: {baseDamage}, Multiplier: {damageMultiplier}, Final: {finalDamage}");
        rootStats.RPC_TakeDamage(finalDamage);
        
        Debug.Log("Trúng bộ phận: " + gameObject.name + " - Sát thương thực tế: " + finalDamage);
    }
}