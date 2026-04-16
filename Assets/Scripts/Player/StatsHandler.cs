using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections;

public class StatsHandler : NetworkBehaviour
{
    private ChangeDetector _changes;
    private bool _deathHandled;
    [Networked] public int Score { get; set; }

    [Header("HP Settings")]
    [Networked] public bool IsDead { get; set; }
    [Networked] public float NetworkHealth { get; set; } = 100f;
    public float maxHealth = 100f;

    [Header("Stamina Settings")]
    public bool IsUsingStamina { get; set; }
    [Networked] public float NetworkStamina { get; set; }
    public float maxStamina = 100f;
    public float staminaRegenRate = 20f;
    [Networked] public TickTimer ExhaustionTimer { get; set; }
    [Networked] public bool IsExhausted { get; set; }

    [Header("Animation")]
    [HideInInspector]
    [SerializeField] private Animator anim;
    [Networked] public float NetworkMoveSpeed { get; set; }
    [Networked] public float NetworkCrouchMove { get; set; } // 0: Đứng im, 1: Di chuyển khi đang ngồi
    [Networked] public bool IsCrouching { get; set; }
    [Networked] public bool IsDancing { get; set; }
    [Networked] public bool IsHealing { get; set; }

    [Networked] public TickTimer LandingDelayTimer { get; set; }
    public bool IsLandingLocked => !LandingDelayTimer.ExpiredOrNotRunning(Runner);
      
    public override void Spawned()
    {
        anim = GetComponentInChildren<Animator>();
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom | ChangeDetector.Source.SnapshotTo);

        if (Object.HasStateAuthority) 
        {
            NetworkHealth = maxHealth;
            NetworkStamina = maxStamina;
        }

        // Fix cho người vào sau: Nếu thấy đối tượng đã chết thì thực thi logic chết luôn
        if (IsDead) HandleDeathLogic();

        if (HasInputAuthority)
        {
            var hud = LocalHUDController.Instance;
            if (hud != null)
                hud.SetTarget(this);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (NetworkHealth <= 0 && !IsDead)
            {
                IsDead = true;
                // Không cần gọi RPC_BroadcastDeath nữa vì ChangeDetector sẽ lo
            }

            // Logic Stamina hồi phục
            if (IsExhausted)
            {
                if (ExhaustionTimer.Expired(Runner))
                {
                    NetworkStamina += staminaRegenRate * Runner.DeltaTime;
                    if (NetworkStamina >= maxStamina)
                    {
                        NetworkStamina = maxStamina;
                        IsExhausted = false;
                    }
                }
            }
            else if(!IsUsingStamina && NetworkStamina < maxStamina)
            {
                NetworkStamina += staminaRegenRate * Runner.DeltaTime;
                NetworkStamina = Mathf.Min(NetworkStamina, maxStamina);
            }
            IsUsingStamina = false;
        }
    }

    public void ConsumingStamina(float amount)
    {
        if (Object.HasStateAuthority && !IsExhausted)
        {
            NetworkStamina -= amount;
            if (NetworkStamina <= 0)
            {
                NetworkStamina = 0;
                IsExhausted = true;
                ExhaustionTimer = TickTimer.CreateFromSeconds(Runner, 1f);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage, PlayerRef killer)
    {
        if (Object.HasStateAuthority && !IsDead)
        {
            NetworkHealth -= damage;
            NetworkHealth = Mathf.Max(0, NetworkHealth);
            
            if (NetworkHealth <= 0)
            {
                IsDead = true;

                // Cộng điểm cho người giết
                if (killer == PlayerRef.None)
                {
                    Debug.LogWarning($"[StatsHandler] Kill has no valid killer ref for {gameObject.name}");
                }
                else if (Runner.TryGetPlayerObject(killer, out var killerObj))
                {
                    var killerStats = killerObj.GetComponent<StatsHandler>();
                    if (killerStats != null)
                    {
                        killerStats.Score += 100;
                        Debug.Log($"[StatsHandler] {killerObj.name} scored 100 points for killing {gameObject.name}. Total: {killerStats.Score}");
                    }
                    else
                    {
                        Debug.LogWarning($"[StatsHandler] Killer object found but StatsHandler missing: {killerObj.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[StatsHandler] Could not resolve killer object for PlayerRef {killer}");
                }
            }
            RPC_ShowBloodEffect(Object.InputAuthority);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ShowBloodEffect(PlayerRef player)
    {
        var hud = LocalHUDController.Instance;
        if (hud != null) hud.TriggerBloodEffect();
    }

    public override void Render()
    {
        if (HasInputAuthority)
        {
            var hud = LocalHUDController.Instance;
            if (hud != null)
                hud.SetBars(NetworkHealth / maxHealth, NetworkStamina / maxStamina);
        }

        // Kiểm tra thay đổi trạng thái chết qua mạng
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(IsDead) && IsDead)
            {
                HandleDeathLogic();
            }
        }
    }

    private void HandleDeathLogic()
    {
        if (_deathHandled) return;
        _deathHandled = true;

        if (anim != null) anim.SetTrigger("Die");
        IsHealing = false;

        // Chết là phải rơi súng, không cần out server.
        var combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.DropWeaponsOnDeath();
            combat.enabled = false;
        }
        
        var mouselook = GetComponent<MouseLook>();
        if (mouselook != null) mouselook.enabled = false;

        if (HasInputAuthority)
        {
            var hud = LocalHUDController.Instance;
            if (hud != null) hud.SetPermanentBloodEffect();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}