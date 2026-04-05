using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;
    public void PlayerJoined(PlayerRef player)
    {
       if(player == Runner.LocalPlayer)
        {
            float rX = Random.Range(-33, 33);
            float rZ = Random.Range(-33, 33);         
            Runner.Spawn(PlayerPrefab, new Vector3(rX, 5, rZ) , Quaternion.identity, player);
        }
    }

}