using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections;

public class StatsHandler : NetworkBehaviour
{
    private PlayerUI _playerUI;
    private ChangeDetector _changes;

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

    [Networked] public TickTimer LandingDelayTimer { get; set; }
    public bool IsLandingLocked => !LandingDelayTimer.ExpiredOrNotRunning(Runner);
      
    public override void Spawned()
    {
        anim = GetComponentInChildren<Animator>();
        _playerUI = GetComponent<PlayerUI>();
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom | ChangeDetector.Source.SnapshotTo);

        if (Object.HasStateAuthority) 
        {
            NetworkHealth = maxHealth;
            NetworkStamina = maxStamina;
        }

        // Fix cho người vào sau: Nếu thấy đối tượng đã chết thì thực thi logic chết luôn
        if (IsDead) HandleDeathLogic();
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
    public void RPC_TakeDamage(float damage)
    {
        if (Object.HasStateAuthority && !IsDead)
        {
            NetworkHealth -= damage;
            NetworkHealth = Mathf.Max(0, NetworkHealth);
            RPC_ShowBloodEffect(Object.InputAuthority);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ShowBloodEffect(PlayerRef player)
    {
        if (_playerUI != null) _playerUI.TriggerBloodEffect();
    }

    public override void Render()
    {
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
        if (anim != null) anim.SetTrigger("Die");

        // Tắt các điều khiển cục bộ
        if (GetComponent<PlayerMovement>() != null) GetComponent<PlayerMovement>().enabled = false;
        if (GetComponent<PlayerCombat>() != null) GetComponent<PlayerCombat>().enabled = false;
        if (GetComponent<CharacterController>() != null) GetComponent<CharacterController>().enabled = false;
        
        var mouselook = GetComponent<MouseLook>();
        if (mouselook != null) mouselook.enabled = false;

        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}