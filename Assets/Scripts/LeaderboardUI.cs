using UnityEngine;
using System.Linq;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI leaderboardText;
    public GameObject leaderboardContainer;
    
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.2f; // Update every 0.2 seconds

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

        // 3. Cập nhật dữ liệu khi đang giữ Tab hoặc theo khoảng thời gian
        if (isHoldingTab)
        {
            UpdateLeaderboardData();
            updateTimer = 0f;
        }
        else
        {
            // Cập nhật định kỳ mỗi UPDATE_INTERVAL giây để đảm bảo dữ liệu luôn được cập nhật
            updateTimer += Time.deltaTime;
            if (updateTimer >= UPDATE_INTERVAL)
            {
                UpdateLeaderboardData();
                updateTimer = 0f;
            }
        }
    }

    void UpdateLeaderboardData()
    {
        var players = FindObjectsOfType<StatsHandler>()
            .OrderByDescending(p => p.killScore)
            .Take(10)
            .ToList();

        string text = "<color=yellow>--- LEADERBOARD ---</color>\n";
        int rank = 1;

        foreach (var p in players)
        {
            string name = p != null ? p.PlayerName.ToString() : "Unknown";
            
            // Highlight màu khác nếu là chính mình (Local Player)
            string color = p.Object.HasInputAuthority ? "green" : "white";
            
            text += $"{rank}. <color={color}>{name}</color>: {p.killScore} kills\n";
            rank++;
        }

        if (leaderboardText != null) 
            leaderboardText.text = text;
    }
}
