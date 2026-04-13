using UnityEngine;
using Photon.Chat;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using Photon.Pun; // Cần thiết để lấy NickName từ PUN 2

public class ChatManager : MonoBehaviour, IChatClientListener
{
    // LỖI 1: Thiếu Instance để ChatUI truy cập
    public static ChatManager Instance { get; private set; }
    
    public ChatUI chatUI;
    private ChatClient chatClient;
    
    [SerializeField] private string chatAppId; 
    public string currentChannel = "General"; // Khớp với kênh mặc định trong Slide

    private bool _isConnectingToChat = false;

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        chatClient = new ChatClient(this);
        // Không connect ngay, sẽ connect sau khi có tên người chơi
    }

    void Update()
    {
        if (chatClient != null) chatClient.Service(); // Duy trì kết nối

        // Connect chat một lần duy nhất sau khi có tên người chơi hợp lệ
        if (!_isConnectingToChat && chatClient != null && !chatClient.CanChat && !string.IsNullOrEmpty(RoomManager.LocalPlayerName) && RoomManager.LocalPlayerName != "Guest")
        {
            ConnectToChat();
            _isConnectingToChat = true;
        }
    }

    private void ConnectToChat()
    {
        string appId = !string.IsNullOrEmpty(chatAppId)
            ? chatAppId
            : PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat;

        if (string.IsNullOrEmpty(appId))
        {
            Debug.LogError("ChatManager: AppIdChat chưa được cấu hình. Vui lòng kiểm tra PhotonServerSettings hoặc field chatAppId.");
            return;
        }

        // Lấy NickName từ RoomManager để đồng bộ tên người chơi
        string userName = RoomManager.LocalPlayerName;
        if (string.IsNullOrEmpty(userName)) userName = "Player" + Random.Range(0, 1000);

        Debug.Log($"ChatManager: Đang kết nối chat với tên '{userName}'");
        chatClient.Connect(appId, "1.0", new AuthenticationValues(userName));
    }

    // --- Giao diện gửi tin nhắn ---

    public void SendChatMessage(string msg)
    {
        if (chatClient == null || !chatClient.CanChat)
        {
            Debug.LogWarning("ChatManager: Không thể gửi tin nhắn vì chưa kết nối tới Photon Chat hoặc chat chưa sẵn sàng.");
            return;
        }

        if (string.IsNullOrEmpty(msg))
        {
            Debug.LogWarning("ChatManager: Tin nhắn trống, không gửi.");
            return;
        }

        Debug.Log($"ChatManager: Gửi tin nhắn '{msg}' tới kênh '{currentChannel}'");
        chatClient.PublishMessage(currentChannel, msg);
    }

    public void SendPrivateMessage(string targetUser, string msg)
    {
        if (chatClient == null || !chatClient.CanChat)
        {
            Debug.LogWarning("ChatManager: Không thể gửi tin nhắn riêng tư vì chưa kết nối tới Photon Chat.");
            return;
        }

        if (string.IsNullOrEmpty(targetUser) || string.IsNullOrEmpty(msg))
        {
            Debug.LogWarning("ChatManager: Tên người nhận hoặc tin nhắn trống.");
            return;
        }

        Debug.Log($"ChatManager: Gửi tin nhắn riêng tư '{msg}' tới '{targetUser}'");
        chatClient.SendPrivateMessage(targetUser, msg);
    }

    // --- IChatClientListener Implementation ---

    public void OnConnected()
    {
        Debug.Log("✓ ChatManager: Đã kết nối Photon Chat!");
        chatClient.Subscribe(new string[] { currentChannel });
        Debug.Log($"ChatManager: Đang subscribe kênh '{currentChannel}'");
    }

    // LỖI 2: Đổi từ 'OnChatStateChanged' thành 'OnChatStateChange'
    public void OnChatStateChange(ChatState state) 
    {
        Debug.Log("Trạng thái Chat: " + state);
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        if (chatUI == null)
        {
            Debug.LogWarning("ChatManager: chatUI chưa được gán, không thể hiển thị tin nhắn.");
            return;
        }

        for (int i = 0; i < senders.Length; i++)
        {
            // Hiển thị lên giao diện thông qua ChatUI
            chatUI.AddMessageToUI(senders[i], messages[i].ToString(), false);
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        if (chatUI == null)
        {
            Debug.LogWarning("ChatManager: chatUI chưa được gán, không thể hiển thị tin nhắn riêng tư.");
            return;
        }

        // Hiển thị tin nhắn riêng tư
        chatUI.AddMessageToUI(sender, message.ToString(), true);
    }

    // Các hàm bắt buộc khác của Interface
    public void OnDisconnected() 
    { 
        Debug.LogWarning("ChatManager: Đã ngắt kết nối Photon Chat.");
        _isConnectingToChat = false; // Cho phép reconnect
    }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) {}
    public void OnSubscribed(string[] channels, bool[] results) 
    { 
        Debug.Log($"ChatManager: Đã subscribe kênh '{string.Join(", ", channels)}' - Kết quả: {string.Join(", ", results)}");
    }
    public void OnUnsubscribed(string[] channels) {}
    public void OnUserSubscribed(string channel, string user) {}
    public void OnUserUnsubscribed(string channel, string user) {}
    public void DebugReturn(DebugLevel level, string message)
    {
        switch (level)
        {
            case DebugLevel.ERROR:
                Debug.LogError($"Photon Chat: {message}");
                break;
            case DebugLevel.WARNING:
                Debug.LogWarning($"Photon Chat: {message}");
                break;
            default:
                Debug.Log($"Photon Chat: {message}");
                break;
        }
    }
}