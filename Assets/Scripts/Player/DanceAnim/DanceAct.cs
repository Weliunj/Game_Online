using Fusion;
using UnityEngine;

public class DanceAct : NetworkBehaviour
{
    private StatsHandler stats;
    private Animator animator;

    public override void Spawned()
    {
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<StatsHandler>();
    }
    void Update()
    {
        if(!Object.HasInputAuthority) return;

        if(stats.IsDead)
        {
            stats.IsDancing = false;
        }

        if(Input.GetKeyDown(KeyCode.Z))
        {
            stats.IsDancing = !stats.IsDancing;
        }
    }

    public override void Render()
    {
        if (animator != null)
        {
            animator.SetBool("Dan1", stats.IsDancing);
        }
    }
}
