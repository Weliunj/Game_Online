using UnityEngine;
using Fusion;
using TMPro; // Bắt buộc phải có để điều khiển Text Mesh Pro

public class PlayerNameHandler : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _nameText; // Kéo Object Text vào đây

    // Thuộc tính mạng: Tự động đồng bộ cho tất cả mọi người
    [Networked]
    public NetworkString<_64> PlayerName { get; set; }

    // Gọi khi nhân vật xuất hiện trên Network
    public override void Spawned()
    {
        // Nếu là nhân vật CỦA CHÍNH MÌNH (HasInputAuthority)
        if (Object.HasInputAuthority)
        {
            // Gán tên từ RoomManager vào biến Networked
            PlayerName = RoomManager.LocalPlayerName;
        }
    }

    // Cập nhật hiển thị (Chạy mỗi Frame để đảm bảo mượt mà)
    public override void Render()
    {
        if (_nameText != null)
    {
        // Gán tên bình thường
        _nameText.text = PlayerName.ToString();

        // NẾU LÀ MÌNH (HasInputAuthority), THÌ ẨN TEXT ĐI
        if (Object.HasInputAuthority)
        {
            _nameText.gameObject.SetActive(false); 
            // Hoặc dùng: _nameText.enabled = false;
        }
    }
    }
}