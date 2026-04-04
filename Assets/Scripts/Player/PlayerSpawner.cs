using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;
    public void PlayerJoined(PlayerRef player)
    {
       if(player == Runner.LocalPlayer)
        {
            float rX = Random.Range(-3, 3);
            float rZ = Random.Range(-3, 3);         
            Runner.Spawn(PlayerPrefab, new Vector3(rX, 3, rZ) , Quaternion.identity, player);
        }
    }

}