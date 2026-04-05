using UnityEngine;
using Fusion;

public class NetworkPlayerShoot : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Transform barrelLocation;
    [SerializeField] private Animator gunAnimator;

    public WeaponData weaponData; 
    private float _nextFireTime; // Biến cục bộ để khống chế tốc độ bắn

    void Update()
    {
        if (!HasInputAuthority) return;

        // Kiểm tra chuột trái + Tốc độ bắn + Không phải đang chết
        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + weaponData.fireRate; // Cập nhật thời gian bắn kế tiếp
            
            // Gọi RPC đồng bộ hình ảnh cho cả phòng
            RPC_ShootEffects(Object.InputAuthority);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ShootEffects(PlayerRef shooter)
    {
        // 1. Chạy Animation súng (Nên để trigger trong Animator súng)
        if (gunAnimator != null) gunAnimator.SetTrigger("Fire");

        // 2. Muzzle Flash (Hiệu ứng đầu nòng)
        if (muzzleFlashPrefab && barrelLocation != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, barrelLocation.position, barrelLocation.rotation);
            Destroy(flash, 0.5f); 
        }

        // 3. Viên đạn (Visual)
        if (bulletPrefab && barrelLocation != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, barrelLocation.position, barrelLocation.rotation);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            
            if (bulletScript != null)
            {
                // Truyền Damage và ID người bắn từ ScriptableObject
                bulletScript.InitBullet(weaponData.damage, shooter);
            }
        }
        
        // 4. Âm thanh (Optional)
        // if (weaponData.shootSound) AudioSource.PlayClipAtPoint(weaponData.shootSound, barrelLocation.position);
    }
}