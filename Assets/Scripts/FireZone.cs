using UnityEngine;
using Fusion;

public class FireZone : MonoBehaviour 
{
    [Header("Cấu hình sát thương")]
    public float damagePerSecond = 10f; 

    [Header("Cấu hình hiệu ứng")]
    public float effectInterval = 0.5f; // Cứ mỗi 0.5s thì nháy đỏ 1 lần
    private float _nextEffectTime;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StatsHandler stats = other.GetComponent<StatsHandler>();
            
            // Chỉ xử lý trên máy nắm quyền điều khiển Player đó (StateAuthority)
            if (stats != null && stats.Object != null && stats.Object.HasStateAuthority)
            {
                // 1. Trừ máu (Dữ liệu Network)
                stats.NetworkHealth -= damagePerSecond * Time.deltaTime;
                stats.NetworkHealth = Mathf.Max(0, stats.NetworkHealth);

                // 2. Kích hoạt Blood Effect (Hiển thị)
                // Chúng ta dùng RPC để báo cho máy của nạn nhân hiện màn hình đỏ
                if (Time.time >= _nextEffectTime && stats.NetworkHealth > 0)
                {
                    // Gọi RPC có sẵn trong StatsHandler để hiện hiệu ứng trên máy nạn nhân
                    stats.RPC_TakeDamage(0); 
                    _nextEffectTime = Time.time + effectInterval;
                }
            }
        }
    }
}