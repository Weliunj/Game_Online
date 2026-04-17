using Fusion;
using UnityEngine;

public class AmmoWorld : NetworkBehaviour
{
    public int ammoAmount = 30;
    public AmmoType ammoType;
    private bool hasPickup = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playercb = other.GetComponent<PlayerCombat>();
            if (playercb != null && !hasPickup)
            {
                hasPickup = true;
                playercb.RPC_PickUpAmmo(ammoAmount, (int)ammoType);
                Runner.Despawn(Object);
            }
        }
    }
}
