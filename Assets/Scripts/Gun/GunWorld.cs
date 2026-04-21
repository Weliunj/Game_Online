using UnityEngine;
using Fusion;

public class GunWorld : NetworkBehaviour
{
    public static readonly System.Collections.Generic.List<GunWorld> AllGuns = new();

    private Rigidbody rb;
    private Collider[] colliders;
    
    [Networked] public NetworkObject ownerObj { get; set; }
    [Networked] public bool hasOwner { get; set; }
    [Networked] public TickTimer pickupTimer { get; set; }
    
    // Đồng bộ vị trí vứt súng để các client khác thấy đúng khi không cầm
    [Networked] public Vector3 networkedPosition { get; set; }
    [Networked] public Quaternion networkedRotation { get; set; }

    public GunData gunData;
    public int ammoRemaining;

    [Header("ZoomImg")]
    public string zoomImgName = "";
    [HideInInspector]
    public GameObject zoomImg = null;

    public override void Spawned()
    {
        if (!AllGuns.Contains(this))
            AllGuns.Add(this);

        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();

        if (gunData.hasZoomImg)
        {
            GameObject hud = GameObject.Find("HUD Canvas"); 
            if (hud != null)
            {
                Transform child = hud.transform.Find("ZoomImg/" + zoomImgName);
                if (child != null) zoomImg = child.gameObject;
            }
        }

        // Chỉ State Authority mới set đạn ban đầu
        if (Object.HasStateAuthority && ammoRemaining <= 0 && gunData != null)
            ammoRemaining = gunData.magSize;
    }

    public override void FixedUpdateNetwork()
    {
        // Kiểm tra nếu chủ sở hữu thoát game (NetworkObject bị Destroy)
        if (Object.HasStateAuthority)
        {
            if (hasOwner && (ownerObj == null || !ownerObj.IsValid))
            {
                RequestDrop(transform.position, transform.rotation);
            }
            
            // Nếu không có chủ, cập nhật vị trí networked để các máy khác đồng bộ
            if (!hasOwner)
            {
                networkedPosition = transform.position;
                networkedRotation = transform.rotation;
            }
        }
    }

    public override void Render()
    {
        if (hasOwner && ownerObj != null && ownerObj.IsValid)
        {
            var combat = ownerObj.GetComponent<PlayerCombat>();
            // Đang vứt súng: RPC đã null equippedGun nhưng hasOwner có thể chưa kịp false → không gắn lại tay
            if (combat != null && combat.GunDropPending)
            {
                DetachFromHandLocal();
                return;
            }
            AttachToHandLocal();
        }
        else
        {
            DetachFromHandLocal();
            // Nếu không có chủ, ép vị trí về đúng vị trí mạng (nếu không dùng NetworkTransform)
            if (!Object.HasStateAuthority)
            {
                transform.position = networkedPosition;
                transform.rotation = networkedRotation;
            }

                // Bật lại mesh cho súng khi nó rớt dưới đất (không có chủ)
                foreach (var r in GetComponentsInChildren<Renderer>())
                {
                    r.enabled = true;
                }
        }
    }

    // Đổi thành OnTriggerEnter để nhạy hơn và tránh lỗi vật lý đẩy Player
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority || hasOwner) return;
        if (!pickupTimer.ExpiredOrNotRunning(Runner)) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            PlayerCombat combat = other.GetComponent<PlayerCombat>();
            
            if (playerNetObj != null && combat != null && !PlayerAlreadyHasGun(playerNetObj))
            {
                hasOwner = true;
                ownerObj = playerNetObj;
            }
        }
    }

    private bool PlayerAlreadyHasGun(NetworkObject playerNetObj)
    {
        for (int i = 0; i < AllGuns.Count; i++)
        {
            var gun = AllGuns[i];
            if (gun == null) continue;
            if (gun == this) continue;
            if (gun.hasOwner && gun.ownerObj == playerNetObj)
                return true;
        }
        return false;
    }

    private void AttachToHandLocal()
    {
        PlayerCombat combat = ownerObj.GetComponent<PlayerCombat>();
        if (combat == null || combat.HandOffset == null) return;

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (colliders != null)
        {
            foreach (var c in colliders)
                c.enabled = false;
        }

        if (transform.parent != combat.HandOffset.transform)
        {
            transform.SetParent(combat.HandOffset.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    private void DetachFromHandLocal()
    {
        if (transform.parent != null)
        {
            var combat = transform.parent.GetComponentInParent<PlayerCombat>();
            if (combat != null && combat.equippedGun == this)
                combat.equippedGun = null;
        }

        if (transform.parent == null) return;

        transform.SetParent(null);

        if (rb != null)
            rb.isKinematic = false;

        if (colliders != null)
        {
            foreach (var c in colliders)
                c.enabled = true;
        }
    }

    public void RequestDrop(Vector3 position, Quaternion rotation)
    {
        if (!Object.HasStateAuthority) return;

        hasOwner = false;
        ownerObj = null;
        pickupTimer = TickTimer.CreateFromSeconds(Runner, 1.0f); // Tăng thời gian để tránh nhặt lại ngay

        transform.position = position;
        transform.rotation = rotation;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDrop(Vector3 position, Quaternion rotation)
    {
        RequestDrop(position, rotation);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        AllGuns.Remove(this);
    }
}