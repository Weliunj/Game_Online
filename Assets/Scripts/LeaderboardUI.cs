using UnityEngine;
using System.Linq;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI leaderboardText;
    public GameObject leaderboardContainer;

    void Start()
    {
        if (leaderboardContainer != null) 
            leaderboardContainer.SetActive(false);
    }

    void Update()
    {
        // 1. Kiểm tra phím Tab
        bool isHoldingTab = Input.GetKey(KeyCode.Tab);

        // 2. Ẩn/Hiện Container
        if (leaderboardContainer != null)
        {
            leaderboardContainer.SetActive(isHoldingTab);
        }

        // 3. Chỉ cập nhật dữ liệu khi đang giữ Tab để tiết kiệm hiệu năng
        if (isHoldingTab)
        {
            UpdateLeaderboardData();
        }
    }

    void UpdateLeaderboardData()
    {
        var players = FindObjectsOfType<StatsHandler>()
            .OrderByDescending(p => p.Score)
            .Take(10)
            .ToList();

        string text = "<color=yellow>--- LEADERBOARD ---</color>\n";
        int rank = 1;

        foreach (var p in players)
        {
            var pUI = p.GetComponent<PlayerUI>();
            string name = pUI != null ? pUI.PlayerName.ToString() : "Unknown";
            
            // Highlight màu khác nếu là chính mình (Local Player)
            string color = p.Object.HasInputAuthority ? "green" : "white";
            
            text += $"{rank}. <color={color}>{name}</color>: {p.Score} PTS\n";
            rank++;
        }

        if (leaderboardText != null) 
            leaderboardText.text = text;
        }
}
