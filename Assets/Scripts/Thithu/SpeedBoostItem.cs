using Fusion;
using UnityEngine;

public class SpeedBoostItem : NetworkBehaviour
{
    public float speedMultiplier = 1.3f;
    public float duration = 3f;

     [Header("Despawn Settings")]
    public float despawnTime = 200f;
    [Networked] private TickTimer despawnTimer { get; set; }
    private bool isCollected;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            despawnTimer = TickTimer.CreateFromSeconds(Runner, despawnTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (despawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        // Trong Shared Mode, logic nhặt chỉ chạy trên Client của Player đang chạm vào
        if (player != null && player.Object.HasStateAuthority)
        {
            isCollected = true;
            player.ApplySpeedBoost(speedMultiplier, duration);
            RPC_CollectItem();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_CollectItem()
    {
        if (Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
}