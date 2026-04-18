using Fusion;
using UnityEngine;

public class MedkitWorld : NetworkBehaviour
{
    public MedkitType medkitType;
    public MedkitData medkitData;
    [Networked] private bool hasPichup { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            var healhd = other.GetComponent<HealHandle>();
            if (healhd != null && !hasPichup)
            {
                hasPichup = true;
                healhd.RPC_PickUpMedkit(1, (int)this.medkitType);
                Runner.Despawn(Object);
            }
        }
    }
}
