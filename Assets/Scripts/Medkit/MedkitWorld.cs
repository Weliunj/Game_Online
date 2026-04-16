using Fusion;
using UnityEngine;

public class MedkitWorld : NetworkBehaviour
{
    public MedkitType medkitType;
    public MedkitData medkitData;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var healhd = other.GetComponent<HealHandle>();
            if (healhd != null)
            {
                healhd.RPC_PickUpMedkit(1, (int)this.medkitType);
                Runner.Despawn(Object);
            }
        }
    }
}
