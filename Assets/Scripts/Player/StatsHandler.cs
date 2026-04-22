using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections;

public class StatsHandler : NetworkBehaviour
{
    private ChangeDetector _changes;
    private bool _deathHandled;
    [Networked] public int killScore { get; set; }

    [Networked] public NetworkString<_32> PlayerName { get; set; }

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

    [Header("Effects")]
    public GameObject deathVFX;

    [Header("Shield")]
    [Networked] public int ShieldCount { get; set; }
    public GameObject shielobj;
    

    [Networked] public TickTimer LandingDelayTimer { get; set; }
    public bool IsLandingLocked => !LandingDelayTimer.ExpiredOrNotRunning(Runner);
      
    public override void Spawned()
    {
        anim = GetComponentInChildren<Animator>();
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom | ChangeDetector.Source.SnapshotTo);

        if (Object.HasInputAuthority)
            PlayerName = RoomManager.LocalPlayerName;

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
                hud.SetStatsHandle(this);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // if (NetworkHealth <= 0 && !IsDead)
            // {
            //     IsDead = true;
            //     // Không cần gọi RPC_BroadcastDeath nữa vì ChangeDetector sẽ lo
            // }

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
        // 1. Chỉ thoát nếu ĐÃ thực sự chết và đã xử lý xong (IsDead đã true từ trước)
        // Bỏ kiểm tra NetworkHealth <= 0 ở đây để cho phép viên đạn cuối cùng đi xuyên qua
        if (!Object.HasStateAuthority || IsDead) return;

        // Xử lý chặn sát thương bằng khiên
        if (damage > 0 && ShieldCount > 0)
        {
            ShieldCount--;
            Debug.Log($"[StatsHandler] {PlayerName} đã chặn sát thương! Khiên còn lại: {ShieldCount}");
            return; // Thoát sớm, hủy toàn bộ sát thương
        }

        // Tìm thông tin người bắn
        StatsHandler killerStats = null;
        foreach (var p in FindObjectsOfType<StatsHandler>())
        {
            if (p.Object.InputAuthority == killer) { killerStats = p; break; }
        }
        
        NetworkString<_32> kName = killerStats != null ? killerStats.PlayerName : (NetworkString<_32>)"Environment";

        // 2. Tính toán máu mới
        float oldHealth = NetworkHealth;
        NetworkHealth -= damage;
        NetworkHealth = Mathf.Max(0, NetworkHealth);

        // Thông báo sát thương và hiệu ứng
        RPC_BroadcastDamage(kName, damage, this.PlayerName);
        RPC_ShowBloodEffect(Object.InputAuthority);

        // 3. KIỂM TRA CÚ CHỐT (Killing Blow)
        // Nếu máu cũ > 0 mà máu mới = 0, thì ĐÂY chính là viên đạn kết liễu
        if (oldHealth > 0 && NetworkHealth <= 0) 
        {
            IsDead = true; // Khóa chết ngay lập tức

            if (killer != PlayerRef.None && killerStats != null)
            {
                // Cộng điểm: Gọi một hàm RPC chuyên biệt trên chính Object của Sát thủ 
                // để đảm bảo quyền ghi dữ liệu (Authority) luôn đúng
                killerStats.RPC_AddScoreFromExternal();
                Debug.Log($"[StatsHandler] Score RPC sent to {killerStats.PlayerName}");
                
                // Phát thông báo Kill
                RPC_BroadcastKill(kName, this.PlayerName, 0);
            }
            else 
            {
                RPC_BroadcastKill("Environment", this.PlayerName, 0);
            }

            //Goi GameManager sau 1 frame để RPC score kịp được xử lý
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinConditionDelayed();
                Debug.Log($"[StatsHandler] CheckWinCondition scheduled");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastDamage(NetworkString<_32> killerName, float damage, NetworkString<_32> victimName)
    {
        if (ChatManager.Instance != null)
        {
            string message = $"<color=orange>{killerName}</color> dealt <color=red>{damage}</color> damage to <color=orange>{victimName}</color>";
            ChatManager.Instance.chatUI.AddMessageToUI("System", message, false);
            Debug.Log($"[Damage] {message}");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastKill(NetworkString<_32> killerName, NetworkString<_32> victimName, int currentKills)
    {
        if (ChatManager.Instance != null && ChatManager.Instance.chatUI != null)
        {
            string killMsg = $"<color=red><b>[KILL]</b></color> <color=yellow>{killerName}</color> killed <color=orange>{victimName}</color>";
            ChatManager.Instance.chatUI.AddMessageToUI("System", killMsg, false);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddScoreFromExternal()
    {
        if (Object.HasStateAuthority)
        {
            this.killScore += 1;
            Debug.Log($"[Score] {PlayerName} score updated to: {killScore}");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ShowBloodEffect(PlayerRef player)
    {
        var hud = LocalHUDController.Instance;
        if (hud != null) hud.TriggerBloodEffect();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceHit(PlayerRef shooter, float damage, PlayerRef target)
    {
        string shooterName = GetPlayerName(shooter);
        string targetName  = GetPlayerName(target);

        Debug.Log($"{shooterName} hit {targetName} for {damage}");

        // Nếu có UI:
        // KillFeedUI.Instance?.Add($"{shooterName} → {targetName}: {damage}");
    }

    private string GetPlayerName(PlayerRef playerRef)
    {
        if (Runner.TryGetPlayerObject(playerRef, out var obj))
        {
            var stats = obj.GetComponent<StatsHandler>();
            return stats != null ? stats.PlayerName.ToString() : "Unknown";
        }
        return "Unknown";
    }

    public override void Render()
    {
        if (HasInputAuthority)
        {
            var hud = LocalHUDController.Instance;
            if (hud != null)
                hud.SetBars(NetworkHealth / maxHealth, NetworkStamina / maxStamina);
        }

        // Bật/tắt hiển thị object khiên bảo vệ tùy thuộc vào số lượng khiên và trạng thái sống
        if (shielobj != null)
        {
            shielobj.SetActive(ShieldCount > 0 && !IsDead);
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

        // Chạy hiệu ứng VFX 1 lần khi chết tại vị trí của Player
        if (deathVFX != null)
        {
            GameObject vfxInstance = Instantiate(deathVFX, transform.position, Quaternion.identity);
            Destroy(vfxInstance, 1f);
        }

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

    public void ApplyShield(int n)
    {
        if (Object.HasStateAuthority)
        {
            ShieldCount += n;
        }
    }
}