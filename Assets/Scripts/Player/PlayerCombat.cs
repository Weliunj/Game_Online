using Fusion;
using UnityEngine;
using System.Collections;

public class PlayerCombat : NetworkBehaviour
{
    public GameObject HandOffset;
    public GameObject DropPos;

    [Header("Current Slots")]
    public GunWorld equippedGun; 
    /// <summary>Local-only: đã bấm vứt súng nhưng trạng thái mạng hasOwner có thể chậm 1 vài tick.</summary>
    public bool GunDropPending;
    public MeleeData meleeInSlot; 

    [Header("Ammo Reserve")]
    public int pistolAmmoReserve = 15;
    public int rifleAmmoReserve = 60;
    public int sniperAmmoReserve = 10;

    [Header("Gun Logic")]
    [SerializeField] private GameObject bulletPrefab;
    private float nextShootTime = 0;
    private bool isReloading = false; // Biến local để khóa input
    private Animator animator;

    // Đồng bộ trạng thái hành động qua mạng
    [Networked] public bool IsShooting { get; set; }
    [Networked] public bool IsReloading { get; set; }
    [Networked] public bool IsZooming { get; set; }

    [Header("Zoom Settings")]
    public GameObject CameraHolder;
    private Vector3 defaultcameraholder;
    private MouseLook mouseLook;
    private float defaultFOV;
    private float defaultSensitivity;

    public override void Spawned()
    {
        animator = GetComponentInChildren<Animator>();
        mouseLook = GetComponent<MouseLook>();

        defaultcameraholder = CameraHolder.transform.localPosition;
        defaultFOV = mouseLook.POV;
        defaultSensitivity = mouseLook.mouseSensitivity;
    }

    public override void Render()
    {
        SyncEquippedGunCache();

        // Cập nhật Animation dựa trên trạng thái Networked (Mượt cho mọi Client)
        if (animator != null)
        {
            animator.SetBool("Shoot", IsShooting);
            animator.SetBool("Reload", IsReloading);
            animator.SetBool("isZooming", IsZooming);
        }

        // Zoom / camera chỉ áp dụng cho người chơi local (tránh dùng equippedGun sai trên proxy)
        if (!Object.HasInputAuthority) return;

        // Lerp camera position, FOV, sensitivity cho zoom mượt
        if (equippedGun != null)
        {
            Vector3 targetPos = IsZooming ? equippedGun.gunData.cameraholder : defaultcameraholder;
            CameraHolder.transform.localPosition = Vector3.Lerp(CameraHolder.transform.localPosition, targetPos, Time.deltaTime * 10f);

            float targetFOV = IsZooming ? equippedGun.gunData.POV : defaultFOV;
            mouseLook._vcam.Lens.FieldOfView = Mathf.Lerp(mouseLook._vcam.Lens.FieldOfView, targetFOV, Time.deltaTime * 10f);

            float targetSensitivity = IsZooming ? defaultSensitivity / equippedGun.gunData.DivmouseSensitivity : defaultSensitivity;
            mouseLook.mouseSensitivity = Mathf.Lerp(mouseLook.mouseSensitivity, targetSensitivity, Time.deltaTime * 10f);

            if (equippedGun.zoomImg != null)
            {
                equippedGun.zoomImg.SetActive(IsZooming);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        SyncEquippedGunCache();
        if (!HasInputAuthority) return;
        
        // Nếu không có súng hoặc đang nạp đạn (local) thì dừng
        if (equippedGun == null || isReloading) 
        {
            // Reset trạng thái bắn nếu bỗng nhiên mất súng hoặc đang nạp
            IsShooting = false;
            IsZooming = false;
            return;
        }
        // Zoommode();
        HandleInput();
    }

    private void HandleInput()
    {
        if (equippedGun == null) return;
        bool inputShoot = equippedGun.gunData.isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        
        if (inputShoot && Time.time > nextShootTime)
        {
            if (equippedGun.ammoRemaining > 0) 
            {
                Shoot();
            }
            else 
            {
                StartCoroutine(ReloadRoutine());
            }
        }
        else if (!inputShoot)
        {
            IsShooting = false;
        }

        // Zoom logic
        IsZooming = Input.GetMouseButton(1);

        if (Input.GetKeyDown(KeyCode.R)) StartCoroutine(ReloadRoutine());
        if (Input.GetKeyDown(KeyCode.G))
        {
            GunDropPending = true;
            RequestDropCurrentGun();
        }
    }

    public void Shoot()
    {
        nextShootTime = Time.time + equippedGun.gunData.fireRate;
        equippedGun.ammoRemaining--;

        Vector3 targetPoint = GetTargetPoint();

        // RPC này chỉ dùng để sinh đạn và hiệu ứng (VFX)
        RPC_ShootEffect(Object.InputAuthority, targetPoint);

        IsShooting = true;
    }

    private Vector3 GetTargetPoint()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        float range = equippedGun.gunData.range;
        int layerMask = ~LayerMask.GetMask("Fire");
        if (Physics.Raycast(ray, out hit, range, layerMask))
        {
            return hit.point;
        }
        return ray.GetPoint(range);
    }

    IEnumerator ReloadRoutine()
    {
        int reserve = GetReserveAmmo(equippedGun.gunData.ammoType);
        if (reserve <= 0 || equippedGun.ammoRemaining == equippedGun.gunData.magSize) yield break;

        // Bật trạng thái nạp đạn
        isReloading = true;
        IsReloading = true;
        IsShooting = false;

        yield return new WaitForSeconds(equippedGun.gunData.reloadTime);

        if (equippedGun != null)
        {
            int ammoNeeded = equippedGun.gunData.magSize - equippedGun.ammoRemaining;
            int ammoToLoad = Mathf.Min(ammoNeeded, reserve);

            equippedGun.ammoRemaining += ammoToLoad;
            SubtractReserveAmmo(equippedGun.gunData.ammoType, ammoToLoad);
        }

        // Tắt trạng thái nạp đạn
        isReloading = false;
        IsReloading = false;

        // Nếu vẫn giữ chuột phải sau khi nạp xong, quay lại zoom
        if (Input.GetMouseButton(1))
        {
            IsZooming = true;
        }
    }

    private int GetReserveAmmo(AmmoType type)
    {
        return type switch
        {
            AmmoType.Pistol => pistolAmmoReserve,
            AmmoType.Rifle => rifleAmmoReserve,
            AmmoType.Sniper => sniperAmmoReserve,
            _ => 0
        };
    }

    private void SubtractReserveAmmo(AmmoType type, int amount)
    {
        if (type == AmmoType.Pistol) pistolAmmoReserve -= amount;
        else if (type == AmmoType.Rifle) rifleAmmoReserve -= amount;
        else if (type == AmmoType.Sniper) sniperAmmoReserve -= amount;
    }

    public void DropGunOnDeath()
    {
        if (equippedGun == null) return;

        StopAllCoroutines();
        isReloading = false;
        IsReloading = false;
        IsShooting = false;

        GunDropPending = true;
        RequestDropCurrentGun();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ShootEffect(PlayerRef shooter, Vector3 targetPoint)
    {
        if (equippedGun != null && equippedGun.firePos != null)
        {
            Vector3 firePosition = equippedGun.firePos.transform.position;
            Vector3 shotDirection = (targetPoint - firePosition).normalized;
            
            Runner.Spawn(bulletPrefab, firePosition, Quaternion.LookRotation(shotDirection), shooter, (runner, obj) => {
                obj.GetComponent<Bullet>().InitBullet(equippedGun.gunData.damage, shooter);
            });

            Debug.DrawLine(firePosition, targetPoint, Color.yellow, 0.1f);
        }
    }

    private void RequestDropCurrentGun()
    {
        if (equippedGun == null) return;

        equippedGun.RPC_RequestDrop(DropPos.transform.position, DropPos.transform.rotation);
        equippedGun = null;
    }

    private void SyncEquippedGunCache()
    {
        equippedGun = null;
        for (int i = 0; i < GunWorld.AllGuns.Count; i++)
        {
            var gun = GunWorld.AllGuns[i];
            if (gun == null) continue;
            if (gun.hasOwner && gun.ownerObj == Object)
            {
                equippedGun = gun;
                GunDropPending = false;
                return;
            }
        }

        // Không còn súng nào sở hữu => clear pending để cho phép nhặt lại.
        GunDropPending = false;
    }
}