using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [Header("Settings")]
    public float speed = 60f;
    public float lifeTime = 2f;
    
    // KHÔNG dùng [Networked] cho các biến này để tránh lỗi truy cập sớm
    private float _damage;
    private PlayerRef _shooterRef;
    private bool _hasHit = false; 
    private TickTimer _destructionTimer;

    public void InitBullet(float dmg, PlayerRef shooter)
    {
        _damage = dmg;
        _shooterRef = shooter;
        _hasHit = false;
        
        // Dùng TickTimer của Fusion để tự hủy sau một khoảng thời gian
        _destructionTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        // Di chuyển viên đạn trong nhịp vật lý của mạng
        if (!_hasHit)
        {
            transform.Translate(Vector3.forward * speed * Runner.DeltaTime);
        }

        // Tự hủy khi hết thời gian
        if (_destructionTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Chỉ xử lý va chạm trên máy có quyền (State Authority) hoặc máy chủ
        if (!Object.HasStateAuthority || _hasHit) return;

        HandleHit(other);
    }

    private void HandleHit(Collider other)
    {
        HitboxPart part = other.GetComponentInParent<HitboxPart>();

        if (part != null)
        {
            // Kiểm tra tránh bắn nhầm mình
            if (part.rootStats.Object.InputAuthority == _shooterRef) return;

            if (!part.rootStats.IsDead)
            {
                _hasHit = true;
                part.OnHit(_damage); // Gọi OnHit để trừ máu
                
                Debug.Log($"✓ [Bullet] Trúng {part.gameObject.name}");
                Runner.Despawn(Object); // Dùng Despawn thay vì Destroy
            }
        }
        else if (other.CompareTag("Player"))
        {
            // Trúng Player nhưng không có HitboxPart → dừng lại để tránh xuyên qua
            _hasHit = true;
            Debug.Log($"[Bullet] Trúng Player: {other.gameObject.name}");
            Runner.Despawn(Object);
        }
        else
        {
            // Trúng môi trường khác
            _hasHit = true;
            Debug.Log($"[Bullet] Trúng vật cản: {other.name}");
            Runner.Despawn(Object);
        }
    }
}