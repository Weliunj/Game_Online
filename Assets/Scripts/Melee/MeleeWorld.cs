using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class MeleeWorld : NetworkBehaviour
{
    public static readonly List<MeleeWorld> AllMelees = new();

    [Header("Components")]
    public Rigidbody rb;
    private Collider[] colliders;

    [Networked] public NetworkObject ownerObj { get; set; }
    [Networked] public bool hasOwner { get; set; }
    [Networked] public TickTimer pickupTimer { get; set; }

    [Networked] public Vector3 networkedPosition { get; set; }
    [Networked] public Quaternion networkedRotation { get; set; }
    public bool useCustomOffset;
    public Vector3 offSetPos;
    public Quaternion offSetRot;

    [Header("Melee Data")]
    public MeleeData meleeData;

    public override void Spawned()
    {
        if (!AllMelees.Contains(this))
            AllMelees.Add(this);

        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();

        // Đồng bộ hóa trạng thái ban đầu
        if (Object.HasStateAuthority && !hasOwner)
        {
            networkedPosition = transform.position;
            networkedRotation = transform.rotation;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // Kiểm tra nếu chủ sở hữu thoát game
            if (hasOwner && (ownerObj == null || !ownerObj.IsValid))
            {
                RequestDrop(transform.position, transform.rotation);
            }

            // Đồng bộ vị trí khi nằm trên đất
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
            // Kiểm tra trạng thái pending từ PlayerCombat (tương tự GunWorld)
            var combat = ownerObj.GetComponent<PlayerCombat>();
            if (combat != null && combat.MeleeDropPending) 
            {
                DetachFromHandLocal();
                return;
            }
            AttachToHandLocal();
        }
        else
        {
            DetachFromHandLocal();
            if (!Object.HasStateAuthority)
            {
                transform.position = networkedPosition;
                transform.rotation = networkedRotation;
            }

            // Hiện lại vũ khí nếu rơi xuống đất
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority || hasOwner) return;
        if (!pickupTimer.ExpiredOrNotRunning(Runner)) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            PlayerCombat combat = other.GetComponent<PlayerCombat>();

            // Kiểm tra nếu người chơi chưa có vũ khí cận chiến
            if (playerNetObj != null && combat != null && combat.meleeInSlot == null)
            {
                hasOwner = true;
                ownerObj = playerNetObj;
                combat.meleeInSlot = meleeData; // Gán dữ liệu vào slot cận chiến
            }
        }
    }

    private void AttachToHandLocal()
    {
        PlayerCombat combat = ownerObj.GetComponent<PlayerCombat>();
        if (combat == null || combat.HandOffset == null) return;

        if (rb != null) rb.isKinematic = true;

        if (colliders != null)
        {
            foreach (var c in colliders) c.enabled = false;
        }

        if (transform.parent != combat.HandOffset.transform)
        {
            transform.SetParent(combat.HandOffset.transform, false);
            if (useCustomOffset)
            {
                transform.localPosition = offSetPos;
                transform.localRotation = offSetRot;
            }
            else
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void DetachFromHandLocal()
    {
        if (transform.parent == null) return;

        // Xóa dữ liệu trong PlayerCombat khi vứt
        var combat = transform.parent.GetComponentInParent<PlayerCombat>();
        if (combat != null)
        {
            if (combat.meleeInSlot == meleeData) combat.meleeInSlot = null;
            if (combat.equippedMelee == this) combat.equippedMelee = null;
        }

        transform.SetParent(null);

        if (rb != null) rb.isKinematic = false;

        if (colliders != null)
        {
            foreach (var c in colliders) c.enabled = true;
        }
    }

    public void RequestDrop(Vector3 position, Quaternion rotation)
    {
        if (!Object.HasStateAuthority) return;

        hasOwner = false;
        ownerObj = null;
        pickupTimer = TickTimer.CreateFromSeconds(Runner, 1.5f); 

        transform.position = position;
        transform.rotation = rotation;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(transform.forward * 2f, ForceMode.Impulse);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDrop(Vector3 position, Quaternion rotation)
    {
        RequestDrop(position, rotation);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        AllMelees.Remove(this);
    }
}