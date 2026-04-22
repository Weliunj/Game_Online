using UnityEngine;
using Fusion;

public class ShieldItem : NetworkBehaviour
{
    public int ShieldCount = 1;

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

        var stats = other.GetComponent<StatsHandler>();
        // Trong Shared Mode, logic nhặt chỉ chạy trên Client của Player đang chạm vào
        if (stats != null && stats.Object.HasStateAuthority)
        {
            isCollected = true;
            stats.ApplyShield(ShieldCount);
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
