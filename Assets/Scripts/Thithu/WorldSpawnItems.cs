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
        // Random.Range cho số nguyên sẽ không bao gồm giá trị max, nên không cần trừ 1
        int randItem = Random.Range(0, itemPrefab.Length);
        Runner.Spawn(
            itemPrefab[randItem],
            spawnPoints[randPos].position,
            Quaternion.identity
        );
    }
}