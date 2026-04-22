using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class FireZone : NetworkBehaviour 
{
    [Header("Cấu hình sát thương")]
    public float damagePerSecond = 10f; 

    [Header("Cấu hình hiệu ứng")]
    public float effectInterval = 0.5f; // Cứ mỗi 0.5s thì nháy đỏ 1 lần

    [Header("Thời gian an toàn (giây)")]
    public float timeToStartDamage = 2f;

    // Dùng Dictionary để quản lý riêng biệt từng người chơi trong vùng lửa
    private Dictionary<StatsHandler, TickTimer> _playersInZone = new Dictionary<StatsHandler, TickTimer>();
    private Dictionary<StatsHandler, float> _nextEffectTimes = new Dictionary<StatsHandler, float>();

    public override void FixedUpdateNetwork()
    {
        List<StatsHandler> players = new List<StatsHandler>(_playersInZone.Keys);

        foreach (StatsHandler stats in players)
        {
            // Bỏ qua nếu người chơi đã chết, bị hủy, hoặc client này không có quyền điều khiển (StateAuthority)
            if (stats == null || stats.Object == null || stats.IsDead || !stats.Object.HasStateAuthority)
            {
                _playersInZone.Remove(stats);
                _nextEffectTimes.Remove(stats);
                continue;
            }

            if (_playersInZone[stats].Expired(Runner))
            {
                // Gây sát thương và nháy đỏ theo từng nhịp (effectInterval)
                float currentTime = Time.time;
                if (!_nextEffectTimes.ContainsKey(stats) || currentTime >= _nextEffectTimes[stats])
                {
                    if (stats.NetworkHealth > 0)
                    {
                        // Lượng máu trừ mỗi nhịp = sát thương/giây * thời gian mỗi nhịp
                        float damageChunk = damagePerSecond * effectInterval;
                        
                        // Giao toàn bộ việc trừ máu, kiểm tra chết, và báo chat cho StatsHandler
                        stats.RPC_TakeDamage(damageChunk, PlayerRef.None); 
                        _nextEffectTimes[stats] = currentTime + effectInterval;
                    }
                }
            }
        }
    }

    // Đổi thành OnTriggerStay để bắt được cả trường hợp người chơi spawn thẳng vào lửa
    private void OnTriggerStay(Collider other)
    {
        StatsHandler stats = other.GetComponent<StatsHandler>();
        if (stats == null) stats = other.GetComponentInParent<StatsHandler>();
        
        if (stats != null && stats.Object != null && stats.Object.HasStateAuthority)
        {
            // Chỉ tạo đếm ngược nếu người chơi này CHƯA có trong danh sách
            if (!_playersInZone.ContainsKey(stats))
            {
                if (Runner != null)
                {
                    // Bắt đầu đếm ngược 2s kể từ lúc người chơi bước vào
                    _playersInZone[stats] = TickTimer.CreateFromSeconds(Runner, timeToStartDamage);
                }
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        StatsHandler stats = other.GetComponent<StatsHandler>();
        if (stats == null) stats = other.GetComponentInParent<StatsHandler>();

        if (stats != null && _playersInZone.ContainsKey(stats))
        {
            _playersInZone.Remove(stats);
            _nextEffectTimes.Remove(stats);
        }
    }
}