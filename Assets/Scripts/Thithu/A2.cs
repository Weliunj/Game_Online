using UnityEngine;

public class A2 : MonoBehaviour
{
    public Transform[] spawnPoint;

    public Transform GetRandom()
    {
        return spawnPoint[Random.Range(0, spawnPoint.Length)];
    }
}
