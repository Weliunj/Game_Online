using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    // Singleton để dễ dàng truy cập từ các script khác
    public static GameManager Instance { get; private set; }

    [Networked] public bool IsGameEnded { get; set; }
    [Networked] private TickTimer _winConditionCheckTimer { get; set; }

    public override void Spawned()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void FixedUpdateNetwork()
    {
        // Kiểm tra timer và thực hiện check win condition khi timer hết
        if (_winConditionCheckTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        if (_winConditionCheckTimer.Expired(Runner))
        {
            Debug.Log("[GameManager] Timer expired, checking win condition now");
            PerformWinConditionCheck();
        }
    }

    // Gọi RPC để kiểm tra điều kiện thắng cuộc với delay (từ StatsHandler)
    public void CheckWinConditionDelayed()
    {
        if (!Object.HasStateAuthority)
        {
            // Nếu không phải state authority, gửi RPC yêu cầu
            RPC_ScheduleWinCheck();
            return;
        }

        Debug.Log("[GameManager] CheckWinConditionDelayed - scheduling check for 2 ticks");
        _winConditionCheckTimer = TickTimer.CreateFromTicks(Runner, 2); // Delay 2 ticks
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ScheduleWinCheck()
    {
        Debug.Log("[GameManager] RPC_ScheduleWinCheck received, scheduling delayed check");
        _winConditionCheckTimer = TickTimer.CreateFromTicks(Runner, 2);
    }

    // Thực hiện kiểm tra win condition
    private void PerformWinConditionCheck()
    {
        Debug.Log($"[GameManager] PerformWinConditionCheck - HasStateAuthority: {Object.HasStateAuthority}, IsGameEnded: {IsGameEnded}");
        
        if (!Object.HasStateAuthority || IsGameEnded) 
        {
            Debug.Log($"[GameManager] Early return");
            return;
        }

        // Tìm tất cả StatsHandler trong scene
        var players = FindObjectsOfType<StatsHandler>().ToList();
        Debug.Log($"[GameManager] Found {players.Count} players in scene");
        
        // Đếm số người còn sống
        var alivePlayers = players.Where(p => !p.IsDead).ToList();
        Debug.Log($"[GameManager] Alive players: {alivePlayers.Count}");

        if (alivePlayers.Count == 1)
        {
            StatsHandler winner = alivePlayers[0];
            Debug.Log($"[GameManager] Winner found: {winner.PlayerName}, killScore: {winner.killScore}");

            IsGameEnded = true;
            Debug.Log($"[GameManager] Calling RPC_ShowVictoryUI");
            RPC_ShowVictoryUI(winner.PlayerName, winner.killScore);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowVictoryUI(NetworkString<_32> winnerName, int kills)
    {
        Debug.Log($"[GameManager] RPC_ShowVictoryUI called - Winner: {winnerName}, Kills: {kills}");
        Debug.Log($"[GameManager] ChatManager.Instance: {ChatManager.Instance}");
        
        if (ChatManager.Instance != null)
        {
            Debug.Log($"[GameManager] ChatManager.Instance.chatUI: {ChatManager.Instance.chatUI}");
            
            if (ChatManager.Instance.chatUI != null)
            {
                string msg = $"\n<color=yellow>*** TRẬN ĐẤU KẾT THÚC ***</color>\n" +
                             $"Người thắng: <color=green>{winnerName}</color>\n" +
                             $"Số người đã hạ: <color=orange>{kills}</color>";
                Debug.Log($"[GameManager] Adding message to chat: {msg}");
                ChatManager.Instance.chatUI.AddMessageToUI("SYSTEM", msg, false);
            }
            else
            {
                Debug.LogError("[GameManager] ChatManager.Instance.chatUI is NULL");
            }
        }
        else
        {
            Debug.LogError("[GameManager] ChatManager.Instance is NULL");
        }
    }
}