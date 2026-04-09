using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public Button sendButton;
    public TextMeshProUGUI chatContent;
    public TMP_InputField privateTargetInput;

    private bool isChatting = false;
    private bool isTypingPrivate = false;

    private void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClicked);
        CloseChat();
    }

    private void Update()
    {
        // 1. Nhấn "/" để mở chat
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            if (!isChatting)
            {
                OpenChat();
            }
        }

        if (!isChatting) return;

        // 2. Enter để gửi
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitChat();
        }

        // 3. ESC để thoát không gửi
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }

        // 4. TAB để chuyển input
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchInputField();
        }
    }

    private void OpenChat()
    {
        isChatting = true;
        isTypingPrivate = false;

        inputField.ActivateInputField();
        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
    }

    private void SwitchInputField()
    {
        if (privateTargetInput == null) return;

        isTypingPrivate = !isTypingPrivate;

        if (isTypingPrivate)
        {
            privateTargetInput.ActivateInputField();
            EventSystem.current.SetSelectedGameObject(privateTargetInput.gameObject);
        }
        else
        {
            inputField.ActivateInputField();
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
    }

    private void SubmitChat()
    {
        if (!string.IsNullOrEmpty(inputField.text))
        {
            SendMessageAction();
        }

        CloseChat();
    }

    private void CloseChat()
    {
        isChatting = false;
        isTypingPrivate = false;

        inputField.text = "";
        if (privateTargetInput != null)
            privateTargetInput.text = "";

        inputField.DeactivateInputField();
        if (privateTargetInput != null)
            privateTargetInput.DeactivateInputField();

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void SendMessageAction()
    {
        string message = inputField.text;
        string target = privateTargetInput != null ? privateTargetInput.text : "";

        if (string.IsNullOrEmpty(target))
        {
            ChatManager.Instance.SendChatMessage(message);
        }
        else
        {
            ChatManager.Instance.SendPrivateMessage(target, message);
        }
    }

    private void OnSendButtonClicked()
    {
        if (isChatting) SubmitChat();
    }

    public void AddMessageToUI(string sender, string msg, bool isPrivate = false)
    {
        string color = isPrivate ? "<color=red>" : "<color=white>";
        string prefix = isPrivate ? "[Private] " : "";
        chatContent.text += $"{color}{prefix}<b>{sender}:</b> {msg}</color>\n";
    }
}