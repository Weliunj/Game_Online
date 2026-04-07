using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [Header("Settings")]
    public float speed = 120f;
    public float lifeTime = 2f;
    
    [Networked] private TickTimer _destructionTimer { get; set; }
    [Networked] private float _damage { get; set; }
    [Networked] private PlayerRef _shooterRef { get; set; }
    [Networked] private bool _hasHitNet { get; set; }

    public void InitBullet(float dmg, PlayerRef shooter)
    {
        _damage = dmg;
        _shooterRef = shooter;
        _hasHitNet = false; // Đảm bảo reset khi mới spawn
        _destructionTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        // 1. Kiểm tra hết hạn (Timer)
        if (_destructionTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // 2. Nếu đã xác nhận trúng ở Tick trước thì không chạy logic nữa
        if (_hasHitNet) return;

        float moveDistance = speed * Runner.DeltaTime;
        if (moveDistance <= 0) moveDistance = 0.1f; 

        Vector3 direction = transform.forward;

        // 3. Dự đoán va chạm
        int layerMask = ~LayerMask.GetMask("Fire");
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, moveDistance, layerMask))
        {
            // Kiểm tra xem có phải trúng chính người bắn không
            // Nếu trúng chính mình thì KHÔNG set _hasHitNet để đạn bay xuyên qua
            bool isSelf = false;
            if (hit.collider.CompareTag("Player"))
            {
                var stats = hit.collider.GetComponentInParent<StatsHandler>();
                if (stats != null && stats.Object.InputAuthority == _shooterRef) isSelf = true;
            }

            if (!isSelf)
            {
                _hasHitNet = true; // Khóa va chạm trên toàn mạng
                transform.position = hit.point;
                HandleHit(hit.collider);
                return;
            }
        }

        // 4. Di chuyển nếu không trúng gì (hoặc trúng chính mình)
        transform.position += direction * moveDistance;
    }

    private void HandleHit(Collider other)
    {
        // Chỉ Server/State Authority mới thực hiện trừ máu và Despawn
        if (!Object.HasStateAuthority) return;

        HitboxPart part = other.GetComponent<HitboxPart>();
        if (part == null) part = other.GetComponentInParent<HitboxPart>();

        if (part != null)
        {
            if (!part.rootStats.IsDead)
            {
                part.OnHit(_damage);
                Debug.Log($"[Hit] {other.name} - Damage: {_damage}");
            }
        }
        else
        {
            Debug.Log($"[Env] Trúng vật cản: {other.name}");
        }

        // Chắc chắn biến mất sau khi đã xử lý xong va chạm hợp lệ
        Runner.Despawn(Object);
    }
}