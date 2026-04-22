using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public string DropItem = "G";
    [HideInInspector] public KeyCode DropItem_KeyCode;

    public string VoiceChat = "V";
    [HideInInspector] public KeyCode VoiceChat_KeyCode;

    void Awake()
    {
        VoiceChat_KeyCode = System.Enum.TryParse(VoiceChat, true, out KeyCode voiceKey) ? voiceKey : KeyCode.None;
        DropItem_KeyCode = System.Enum.TryParse(DropItem, true, out KeyCode dropKey) ? dropKey : KeyCode.None;
    }
}
