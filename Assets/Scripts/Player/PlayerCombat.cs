using Fusion;
using UnityEngine;
using System.Collections;

public class PlayerCombat : NetworkBehaviour
{
    private StatsHandler stats;
    public GameObject HandOffset;
    public GameObject DropPos;

    [Header("Current Slots")]
    
    public GunWorld equippedGun; 
    public bool GunDropPending;
    public MeleeWorld equippedMelee; 
    public bool MeleeDropPending;
    public MeleeData FistInSlot; 
    public MeleeData meleeInSlot;


    [Header("Ammo Reserve")]
    public int pistolAmmoReserve = 15;
    public int rifleAmmoReserve = 60;
    public int sniperAmmoReserve = 10;
    public int smgAmmoReserve = 100;
    public int shotgunAmmoReserve = 20;

    [Header("Aim Debug")]
    public bool enableAimDebug = true;
    private bool isAimHitPlayer = false;

    [Header("Gun Logic")]
    public MeshRenderer Hat;
    public SkinnedMeshRenderer body;
    [SerializeField] private GameObject bulletPrefab;
    private float nextShootTime = 0;
    private bool isReloading = false; // Biến local để khóa input
    private Animator animator;

    [Header("Melee Logic")]
    private float nextMeleeTime = 0;

    // Đồng bộ trạng thái hành động qua mạng
    [Networked] public bool IsShooting { get; set; }
    [Networked] public bool IsReloading { get; set; }
    [Networked] public bool IsZooming { get; set; }
    [Networked] public int curSlot { get; set; } // 1 = Cận chiến, 2 = Súng

    [Header("Zoom Settings")]
    public GameObject CameraHolder;
    private Vector3 defaultcameraholder;
    private MouseLook mouseLook;
    private float defaultFOV;
    private float defaultSensitivity;

    public override void Spawned()
    {
        stats = GetComponent<StatsHandler>();
        animator = GetComponentInChildren<Animator>();
        mouseLook = GetComponent<MouseLook>();

        defaultcameraholder = CameraHolder.transform.localPosition;
        defaultFOV = mouseLook.POV;
        defaultSensitivity = mouseLook.mouseSensitivity;

        // Đặt slot mặc định là 1 khi người chơi xuất hiện
        if (Object.HasStateAuthority)
        {
            curSlot = 1;
        }
    }

    public override void Render()
    {
        SyncEquippedWeaponsCache();

        // Cập nhật Animation dựa trên trạng thái Networked (Mượt cho mọi Client)
        if (animator != null)
        {
            animator.SetBool("Shoot", IsShooting);
            animator.SetBool("Reload", IsReloading);
            animator.SetBool("isZooming", IsZooming);
        }

        // --- ĐỒNG BỘ ẨN/HIỆN VŨ KHÍ QUA MẠNG CHUNG CHO MỌI CLIENT ---
        if (equippedGun != null)
        {
            foreach (var r in equippedGun.GetComponentsInChildren<Renderer>())
            {
                // Nếu curSlot == 2 (Súng) thì hiện, ngược lại thì ẩn
                r.enabled = (curSlot == 2);
            }
        }
        if (equippedMelee != null)
        {
            foreach (var r in equippedMelee.GetComponentsInChildren<Renderer>())
            {
                // Nếu curSlot == 1 (Cận chiến) thì hiện, ngược lại thì ẩn
                r.enabled = (curSlot == 1);
            }
        }

        // Zoom / camera chỉ áp dụng cho người chơi local (tránh dùng equippedGun sai trên proxy)
        if (!Object.HasInputAuthority) return;

        if (equippedGun != null && curSlot == 2)
        {
            if (enableAimDebug)
            {
                EvaluateAimDebug();
            }
            else
            {
                UpdateCrosshairColor(false);
            }
        }
        else
        {
            UpdateCrosshairColor(false);
        }

        // Lerp camera position, FOV, sensitivity cho zoom mượt
        if (equippedGun != null && curSlot == 2) // Chỉ áp dụng zoom/hiển thị tâm ngắm khi đang cầm súng
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
        else
        {
            // Trả camera về mặc định và tắt tâm ngắm nếu đang cất súng
            CameraHolder.transform.localPosition = Vector3.Lerp(CameraHolder.transform.localPosition, defaultcameraholder, Time.deltaTime * 10f);
            mouseLook._vcam.Lens.FieldOfView = Mathf.Lerp(mouseLook._vcam.Lens.FieldOfView, defaultFOV, Time.deltaTime * 10f);
            mouseLook.mouseSensitivity = Mathf.Lerp(mouseLook.mouseSensitivity, defaultSensitivity, Time.deltaTime * 10f);

            if (equippedGun != null && equippedGun.zoomImg != null)
            {
                equippedGun.zoomImg.SetActive(false);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        SyncEquippedWeaponsCache();
        if (!HasInputAuthority) return;

        if (equippedGun != null)
        {
            // Lấy tâm màn hình
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            // Vẽ tia màu xanh lá cây dài đúng bằng Range của súng
            Debug.DrawRay(ray.origin, ray.direction * equippedGun.gunData.range, Color.green);
        }
        // --- XỬ LÝ ĐỔI SLOT VŨ KHÍ ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Đang ở 1 bấm thêm 1 nữa thì vẫn là 1
            curSlot = 1;
            Debug.Log("CurSlot: " + curSlot);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Đang ở 2 bấm phát nữa thì nhảy về 1, nếu không thì chuyển sang 2
            curSlot = 2;
            Debug.Log("CurSlot: " + curSlot);
        }
        
        // --- XỬ LÝ VỨT VŨ KHÍ ---
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (curSlot == 2 && equippedGun != null)
            {
                stats.IsDancing = false;
                GunDropPending = true;
                RequestDropCurrentGun();
            }
            else if (curSlot == 1 && equippedMelee != null)
            {
                stats.IsDancing = false;
                MeleeDropPending = true;
                RequestDropCurrentMelee();
            }
        }

        HandleInput();
    }

    private void HandleInput()
    {
        // KHÔNG cho phép nhận Input đánh/bắn nếu chuột đang được mở (hiện con trỏ)
        if (mouseLook != null && !mouseLook.IsCursorLocked)
        {
            IsShooting = false;
            IsZooming = false;
            ResetZoomRenderers();
            return;
        }

        if (curSlot == 2)
        {
            if (equippedGun == null || isReloading) 
            {
                IsShooting = false;
                IsZooming = false;
                ResetZoomRenderers();
                return;
            }

            bool inputShoot = equippedGun.gunData.isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            
            if (inputShoot && Time.time > nextShootTime)
            {
                if (equippedGun.ammoRemaining > 0) 
                {
                    stats.IsDancing = false;
                    if (mouseLook != null) mouseLook.AlignPlayerWithCamera();
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
            if (IsZooming)  
            { 
                stats.IsDancing = false; 
                if (mouseLook != null) mouseLook.AlignPlayerWithCamera();
                SetZoomRenderers(true);
            }
            else
            {
                SetZoomRenderers(false);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                stats.IsDancing = false;
                StartCoroutine(ReloadRoutine());
            }
        }
        else if (curSlot == 1)
        {
            // Đảm bảo cất súng thì sẽ không zoom hay bắn nạp đạn nữa
            IsShooting = false;
            IsZooming = false;
            ResetZoomRenderers();

            // Đổi thành GetMouseButton(0) để có thể giữ chuột chém liên tục, tránh lỗi hụt nhịp click
            if (Input.GetMouseButton(0) && Time.time > nextMeleeTime)
            {
                stats.IsDancing = false;
                if (mouseLook != null) mouseLook.AlignPlayerWithCamera();
                MeleeAttack();
            }
        }
    }

    private void SetZoomRenderers(bool zooming)
    {
        if (!Object.HasInputAuthority || equippedGun == null || !equippedGun.gunData.hasZoomImg)
            return;

        var mode = zooming ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
        if (Hat != null) Hat.shadowCastingMode = mode;
        if (body != null) body.shadowCastingMode = mode;

        var gun = equippedGun.transform;
        foreach (var r in gun.GetComponentsInChildren<MeshRenderer>())
        {
            r.shadowCastingMode = mode;
        }
    }

    private void ResetZoomRenderers()
    {
        SetZoomRenderers(false);
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

    private void EvaluateAimDebug()
    {
        isAimHitPlayer = false;

        if (Camera.main == null || equippedGun == null)
        {
            UpdateCrosshairColor(false);
            return;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        float range = equippedGun.gunData.range;
        int layerMask = ~LayerMask.GetMask("Fire");
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range, layerMask))
        {
            if (hit.collider != null)
            {
                var hitbox = hit.collider.GetComponent<HitboxPart>() ?? hit.collider.GetComponentInParent<HitboxPart>();
                var stats = hit.collider.GetComponent<StatsHandler>() ?? hit.collider.GetComponentInParent<StatsHandler>();

                if (hitbox != null && hitbox.rootStats != null && !hitbox.rootStats.IsDead && hitbox.rootStats.Object.InputAuthority != Object.InputAuthority)
                {
                    isAimHitPlayer = true;
                }
                else if (stats != null && stats.Object.InputAuthority != Object.InputAuthority)
                {
                    isAimHitPlayer = true;
                }
            }
        }

        UpdateCrosshairColor(isAimHitPlayer);
    }

    private void UpdateCrosshairColor(bool hitPlayer)
    {
        if (LocalHUDController.Instance != null)
        {
            LocalHUDController.Instance.SetCrosshairColor(hitPlayer ? Color.red : Color.black);
        }
    }

    private void MeleeAttack()
    {
        // Ưu tiên dùng vũ khí nhặt được, nếu không có thì dùng tay không (FistInSlot)
        MeleeData currentMelee = meleeInSlot != null ? meleeInSlot : FistInSlot;
        if (currentMelee == null) return;

        var stats = GetComponent<StatsHandler>();
        if (stats != null && stats.NetworkStamina < currentMelee.staminaCost) return;

        if (stats != null)
        {
            stats.IsUsingStamina = true;
            stats.ConsumingStamina(currentMelee.staminaCost);
        }

        nextMeleeTime = Time.time + currentMelee.attackRate;
        int randomAnim = Random.Range(1, 3); // Trả về ngẫu nhiên 1 hoặc 2

        bool isWeapon = (equippedMelee != null);
        RPC_PlayMeleeAnim(randomAnim, isWeapon);

        if (isWeapon)
        {
            StartCoroutine(MeleeHitDelayRoutine(currentMelee, 0.2f));
        }
        else
        {
            ProcessMeleeHit(currentMelee);
        }
    }

    private IEnumerator MeleeHitDelayRoutine(MeleeData currentMelee, float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessMeleeHit(currentMelee);
    }

    private void ProcessMeleeHit(MeleeData currentMelee)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int layerMask = ~LayerMask.GetMask("Fire");

        // --- DEBUG: vẽ tia ---
        Debug.DrawRay(ray.origin, ray.direction * currentMelee.distance, Color.red, 2f);

        // --- SPHERECAST ---
        RaycastHit[] hits = Physics.SphereCastAll(
        ray,
        currentMelee.radius,     // bán kính hình cầu
        currentMelee.distance,   // chiều dài tia
        layerMask
    );

        float closestDist = float.MaxValue;
        HitboxPart targetPart = null;

        foreach (var hit in hits)
        {
            HitboxPart part = hit.collider.GetComponent<HitboxPart>();
            if (part == null) part = hit.collider.GetComponentInParent<HitboxPart>();

            if (part != null && !part.rootStats.IsDead)
            {
                if (part.rootStats.Object.InputAuthority == Object.InputAuthority) continue;

                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    targetPart = part;
                }
            }
        }

        if (targetPart != null)
        {
            Debug.Log($"[Melee SphereCast] Đã đánh trúng: {targetPart.gameObject.name}");
            targetPart.OnHit(currentMelee.damage, Object.InputAuthority);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayMeleeAnim(int combo, bool isWeapon)
    {
        if (animator != null)
        {
            string animPrefix = isWeapon ? "MeleeAtk" : "FistAtk";
            animator.SetTrigger(animPrefix + combo);
        }
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
            AmmoType.Smg => smgAmmoReserve,
            AmmoType.Shotgun => shotgunAmmoReserve,
            _ => 0
        };
    }

    private void SubtractReserveAmmo(AmmoType type, int amount)
    {
        if (type == AmmoType.Pistol) pistolAmmoReserve -= amount;
        else if (type == AmmoType.Rifle) rifleAmmoReserve -= amount;
        else if (type == AmmoType.Sniper) sniperAmmoReserve -= amount;
        else if (type == AmmoType.Smg) smgAmmoReserve -= amount;
        else if (type == AmmoType.Shotgun) shotgunAmmoReserve -= amount;
    }

    public void DropWeaponsOnDeath()
    {
        StopAllCoroutines();
        isReloading = false;
        IsReloading = false;
        IsShooting = false;

        if (equippedGun != null)
        {
            GunDropPending = true;
            RequestDropCurrentGun();
        }
        if (equippedMelee != null)
        {
            MeleeDropPending = true;
            RequestDropCurrentMelee();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ShootEffect(PlayerRef shooter, Vector3 targetPoint)
    {
        if (equippedGun != null && equippedGun.firePos != null)
        {
            Vector3 firePosition = equippedGun.firePos.transform.position;
            Vector3 shotDirection = (targetPoint - firePosition).normalized;
            
            Runner.Spawn(bulletPrefab, firePosition, Quaternion.LookRotation(shotDirection), shooter, (runner, obj) => {
                obj.GetComponent<Bullet>().InitBullet(equippedGun.gunData.damage, shooter, equippedGun.gunData.range);
            });

            Debug.DrawLine(firePosition, targetPoint, Color.yellow, 0.1f);
        }
    }

    private void RequestDropCurrentGun()
    {
        if (equippedGun == null) return;

        // Bật lại mesh cho súng trước khi vứt (tránh lỗi tàng hình nếu đang cất súng lúc chết)
        foreach (var r in equippedGun.GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
        }

        equippedGun.RPC_RequestDrop(DropPos.transform.position, DropPos.transform.rotation);
        equippedGun = null;
    }

    private void RequestDropCurrentMelee()
    {
        if (equippedMelee == null) return;

        // Bật lại mesh trước khi vứt
        foreach (var r in equippedMelee.GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
        }

        equippedMelee.RPC_RequestDrop(DropPos.transform.position, DropPos.transform.rotation);
        equippedMelee = null;
        meleeInSlot = null; // Trả về tay không (FistInSlot)
    }

    private void SyncEquippedWeaponsCache()
    {
        // --- ĐỒNG BỘ SÚNG ---
        if (GunDropPending)
        {
            bool stillOwns = false;
            for (int i = 0; i < GunWorld.AllGuns.Count; i++)
            {
                var gun = GunWorld.AllGuns[i];
                if (gun != null && gun.hasOwner && gun.ownerObj == Object)
                {
                    stillOwns = true;
                    break;
                }
            }
            if (!stillOwns) GunDropPending = false; // Server đã xác nhận rớt hoàn toàn
        }
        else
        {
            equippedGun = null;
            for (int i = 0; i < GunWorld.AllGuns.Count; i++)
            {
                var gun = GunWorld.AllGuns[i];
                if (gun == null) continue;
                if (gun.hasOwner && gun.ownerObj == Object)
                {
                    equippedGun = gun;
                    break;
                }
            }
        }

        // --- ĐỒNG BỘ CẬN CHIẾN ---
        if (MeleeDropPending)
        {
            bool stillOwnsMelee = false;
            for (int i = 0; i < MeleeWorld.AllMelees.Count; i++)
            {
                var melee = MeleeWorld.AllMelees[i];
                if (melee != null && melee.hasOwner && melee.ownerObj == Object)
                {
                    stillOwnsMelee = true;
                    break;
                }
            }
            if (!stillOwnsMelee) MeleeDropPending = false;
        }
        else
        {
            equippedMelee = null;
            for (int i = 0; i < MeleeWorld.AllMelees.Count; i++)
            {
                var melee = MeleeWorld.AllMelees[i];
                if (melee == null) continue;
                if (melee.hasOwner && melee.ownerObj == Object)
                {
                    equippedMelee = melee;
                    meleeInSlot = melee.meleeData; // Đồng bộ Data
                    break;
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickUpAmmo(int amount, int type)
    {
        if (!Object.HasStateAuthority) return;
        if (!System.Enum.IsDefined(typeof(AmmoType), type))
            return;

        AmmoType ammoType = (AmmoType)type;
        switch (ammoType)
        {
            case AmmoType.Pistol: pistolAmmoReserve += amount; break;
            case AmmoType.Rifle: rifleAmmoReserve += amount; break;
            case AmmoType.Sniper: sniperAmmoReserve += amount; break;
            case AmmoType.Smg: smgAmmoReserve += amount; break;
            case AmmoType.Shotgun: shotgunAmmoReserve += amount; break;
        }
    }
}