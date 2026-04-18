using Fusion;
using UnityEngine;

public class MedkitPack : NetworkBehaviour
{
    public GameObject[] medkitModels;
    public int minitemAmount = 2;
    public int maxitemAmount = 7;
    [Networked] private bool hasSpawnedMedkits { get; set; }

    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (!other.CompareTag("Player")) return;
        if (hasSpawnedMedkits) return;

        hasSpawnedMedkits = true;
        int finalitemAmount = Random.Range(minitemAmount, maxitemAmount + 1);
        for (int i = 0; i < finalitemAmount; i++)
        {
            int randomIndex = Random.Range(0, medkitModels.Length);
            var networkMedkit = Runner.Spawn(medkitModels[randomIndex], transform.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), Quaternion.identity);
            GameObject medkit = networkMedkit.gameObject;
            Rigidbody rb = medkit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDirection = Random.onUnitSphere;
                randomDirection.y = Mathf.Abs(randomDirection.y); // Đảm bảo hướng lên trên
                rb.AddForce(randomDirection * Random.Range(1f, 3f), ForceMode.Impulse);
            }
        }

        Runner.Despawn(Object);
    }

}
