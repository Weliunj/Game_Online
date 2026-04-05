using UnityEngine;
using Fusion;

public class PlayerCombat : NetworkBehaviour
{
    private Animator _anim;

    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePos; // Đầu nòng súng
    public WeaponData weaponData; 
    private float _nextFireTime;

    public override void Spawned()
    {
        _anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + weaponData.fireRate;
            
            // 1. Tìm điểm mục tiêu bằng Raycast từ giữa màn hình
            Vector3 targetPoint = GetTargetPoint();

            // 2. Gửi điểm này qua RPC để mọi máy đều sinh đạn bay về đó
            RPC_Shoot(Object.InputAuthority, targetPoint);
        }
        
        _anim.SetBool("Shoot", Input.GetButton("Fire1"));
    }

    private Vector3 GetTargetPoint()
    {
        // 1. Raycast từ tâm Camera
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 target;

        if (Physics.Raycast(ray, out hit, weaponData.range))
        {
            target = hit.point;
            // Vẽ tia từ Camera đến điểm trúng (Màu Vàng)
            Debug.DrawLine(ray.origin, hit.point, Color.yellow, 0.1f);
        }
        else
        {
            target = ray.GetPoint(weaponData.range);
        }

        // --- DEBUG QUAN TRỌNG: Tia từ súng đến mục tiêu ---
        // Vẽ tia từ đầu nòng súng (firePos) đến điểm mục tiêu (Màu Xanh Lá)
        // Tia này chính là đường bay thực tế của viên đạn
        Debug.DrawLine(firePos.position, target, Color.green, 0.5f);

        return target;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_Shoot(PlayerRef shooter, Vector3 targetPoint)
    {
        if (bulletPrefab && firePos != null)
        {
            // TÍNH TOÁN HƯỚNG: Lấy (Điểm trúng - Đầu nòng) để ra hướng chuẩn
            Vector3 bulletDirection = (targetPoint - firePos.position).normalized;
            Quaternion bulletRotation = Quaternion.LookRotation(bulletDirection);

            // Spawn bullet qua Network.Spawn thay vì Instantiate
            NetworkObject bulletNO = Runner.Spawn(bulletPrefab, firePos.position, bulletRotation, Object.InputAuthority);
            Bullet bulletScript = bulletNO.GetComponent<Bullet>();

            if (bulletScript != null)
            {
                bulletScript.InitBullet(weaponData.damage, shooter);
            }
        }

        if (_anim != null) _anim.SetTrigger("Shoot");
    }
}