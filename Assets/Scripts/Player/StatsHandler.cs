using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections;

public class StatsHandler : NetworkBehaviour
{
    // Tham chiếu đến UI để kích hoạt hiệu ứng
    private PlayerUI _playerUI;
    private PlayerMovement playermove;

    [Header("HP Settings")]
    [Networked] public float NetworkHealth { get; set; } = 100f;
    public float maxHealth = 100f;

    [Header("Stamina Settings")]
    public bool IsUsingStamina { get; set; }
    [Networked] public float NetworkStamina { get; set; }
    public float maxStamina = 100f;
    public float staminaRegenRate = 20f;
    [Networked] public TickTimer ExhaustionTimer { get; set; }
    [Networked] public bool IsExhausted { get; set; }

    public override void Spawned()
    {
        _playerUI = GetComponent<PlayerUI>();
        playermove = GetComponent<PlayerMovement>();

        if (Object.HasStateAuthority) 
        {
            NetworkHealth = maxHealth;
            NetworkStamina = maxStamina;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if(NetworkHealth <= 0) Destroy(gameObject);
            // Nếu đang kiệt sức
            if (IsExhausted)
            {
                // Kiểm tra xem đã hết 1 giây chờ chưa
                if (ExhaustionTimer.Expired(Runner))
                {
                    // Bắt đầu hồi stamina
                    NetworkStamina += staminaRegenRate * Runner.DeltaTime;
                    
                    // Nếu đã hồi đầy 100%
                    if (NetworkStamina >= maxStamina)
                    {
                        NetworkStamina = maxStamina;
                        IsExhausted = false; // Hết trạng thái kiệt sức, cho phép dùng tiếp
                    }
                }
            }
            else if(!IsUsingStamina) // Trạng thái bình thường
            {
                if (NetworkStamina < maxStamina)
                {
                    NetworkStamina += staminaRegenRate * Runner.DeltaTime;
                    NetworkStamina = Mathf.Min(NetworkStamina, maxStamina);
                }
            }
            IsUsingStamina = false;
        }
    }

    public void ConsumingStamina(float amount)
    {
        if (Object.HasStateAuthority && !IsExhausted)
        {
            NetworkStamina -= amount;
            
            // Nếu chạm đáy 0
            if (NetworkStamina <= 0)
            {
                NetworkStamina = 0;
                IsExhausted = true; // Kích hoạt trạng thái kiệt sức

                // Đặt bộ đếm thời gian chờ 1 giây
                ExhaustionTimer = TickTimer.CreateFromSeconds(Runner, 1f);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage)
    {
        NetworkHealth -= damage;
        NetworkHealth = Mathf.Max(0, NetworkHealth);

        // Gửi lệnh hiện máu đến máy của nạn nhân
        RPC_ShowBloodEffect(Object.InputAuthority);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ShowBloodEffect(PlayerRef player)
    {
        // Gọi hàm trigger hiệu ứng bên PlayerUI
        if (_playerUI != null) _playerUI.TriggerBloodEffect();
    }
}