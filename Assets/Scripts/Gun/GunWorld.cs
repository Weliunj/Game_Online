using UnityEngine;
using Fusion;

public class GunWorld : NetworkBehaviour
{
    public Rigidbody rb;
    public GameObject firePos;
    
    // Đổi sang dùng NetworkObject để lưu chủ sở hữu thay vì RPC
    [Networked] public NetworkObject ownerObj { get; set; }
    [Networked] public bool hasOwner { get; set; } 
    [Networked] public TickTimer pickupTimer { get; set; }

    public GunData gunData;
    public int ammoRemaining;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        if (Object.HasStateAuthority && ammoRemaining <= 0 && gunData != null)
            ammoRemaining = gunData.magSize; 
    }

    public override void Render()
    {
        // Nếu có chủ, thực hiện gắn súng vào tay (Chạy trên tất cả các Client)
        if (hasOwner && ownerObj != null)
        {
            AttachToHandLocal();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Kiểm tra Authority và súng đã có chủ chưa
        if (!Object.HasStateAuthority || hasOwner) return;

        // 2. KIỂM TRA TIMER: Nếu timer chưa chạy xong thì không cho nhặt
        if (!pickupTimer.ExpiredOrNotRunning(Runner)) return;

        if (other.CompareTag("Player"))
        {
            PlayerCombat combat = other.GetComponent<PlayerCombat>();
            StatsHandler stats = other.GetComponent<StatsHandler>();
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            
            if (combat != null && playerNetObj != null && combat.equippedGun == null && (stats == null || !stats.IsDead))
            {
                // Chỉ cần gán dữ liệu mạng, hàm Render sẽ tự lo phần hình ảnh
                hasOwner = true;
                ownerObj = playerNetObj;
            }
        }
    }

    private void AttachToHandLocal()
    {
        // Logic hiển thị trên từng máy
        PlayerCombat combat = ownerObj.GetComponent<PlayerCombat>();
        if (combat == null || combat.HandOffset == null) return;

        if (rb != null) rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;

        // Nếu súng chưa dính vào tay thì mới SetParent
        if (transform.parent != combat.HandOffset.transform)
        {
            transform.SetParent(combat.HandOffset.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            combat.equippedGun = this;
        }
    }

    public void RequestDrop(Vector3 position, Quaternion rotation)
    {
        hasOwner = false;
        ownerObj = null;
        
        // THIẾT LẬP KHÓA NHẶT ĐỒ TRONG 0.5 GIÂY
        pickupTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);

        transform.SetParent(null);
        transform.position = position;
        transform.rotation = rotation;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(transform.forward * 2f, ForceMode.Impulse);
        }
        
        GetComponent<Collider>().enabled = true;
    }
}