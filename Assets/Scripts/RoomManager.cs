using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    private NetworkRunner _runner; 

    [Header("UI Setup")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;
    public GameObject menuPanel;
    public GameObject hubPanel;

    [Header("Spawning")]
    public NetworkObject playerPrefab; 
    public GameObject spawnPos;

    public static string LocalPlayerName = "Guest"; 

    private void Start()
    {
        // Vì RoomManager là MonoBehaviour, dùng Start để khởi tạo UI
        if (menuPanel != null) menuPanel.SetActive(true);
        if (hubPanel != null) hubPanel.SetActive(false);
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) && menuPanel != null && menuPanel.activeSelf)
        {
            // Gọi hàm JoinOrCreateRoom mà bạn đã viết
            JoinOrCreateRoom();
        }
    }
    public async void JoinOrCreateRoom()
    {
        LocalPlayerName = !string.IsNullOrEmpty(playerNameInput.text) 
            ? playerNameInput.text 
            : "Guest_" + Random.Range(100, 999);

        string finalRoomName = string.IsNullOrEmpty(roomNameInput.text) 
            ? "Public_Lobby" 
            : roomNameInput.text;

        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = finalRoomName, 
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            // Fix: Dùng dấu ngoặc để cả 2 dòng đều chạy khi kết nối OK
            if (menuPanel != null) menuPanel.SetActive(false);
            if (hubPanel != null) hubPanel.SetActive(true);

            await System.Threading.Tasks.Task.Delay(100); 

            if (_runner.IsRunning)
            {
                float x = Random.Range(-5f, 5f);
                float z = Random.Range(-5f, 5f);
                _runner.Spawn(playerPrefab, spawnPos.transform.position + new Vector3(x, 0, z), Quaternion.identity, _runner.LocalPlayer);
            }
        }
    }
}