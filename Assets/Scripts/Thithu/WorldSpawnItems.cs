using Fusion;
using UnityEngine;

public class WorldSpawnItems : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public NetworkObject[] itemPrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 10f;

    [Networked] private TickTimer spawnTimer { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (spawnTimer.Expired(Runner))
        {
            SpawnItem();
            spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }

    void SpawnItem()
    {
        int randPos = Random.Range(0, spawnPoints.Length);
        int randItem = Random.Range(0, itemPrefab.Length - 1);
        Runner.Spawn(
            itemPrefab[randItem],
            spawnPoints[randPos].position,
            Quaternion.identity
        );
    }
}