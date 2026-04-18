using Fusion;
using UnityEngine;

public class AmmoWorld : NetworkBehaviour
{
    public int ammoAmount = 30;
    public AmmoType ammoType;
    [Networked] private bool hasPichup { get; set; }
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            var playercb = other.GetComponent<PlayerCombat>();
            if (playercb != null && !hasPichup)
            {
                hasPichup = true;
                playercb.RPC_PickUpAmmo(ammoAmount, (int)ammoType);
                Runner.Despawn(Object);
            }
        }
    }
}
