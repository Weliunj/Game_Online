using Fusion;
using UnityEngine;

public class VoiceChat : NetworkBehaviour
{
    private PlayerInput input;
    public GameObject voiceChatUI;

    [Networked, OnChangedRender(nameof(OnTalkingChanged))]
    public NetworkBool IsTalking { get; set; }

    public override void Spawned()
    {
        input = GetComponent<PlayerInput>();
        if (voiceChatUI != null)
            voiceChatUI.SetActive(false);
            
        if (HasInputAuthority && LocalHUDController.Instance != null)
            LocalHUDController.Instance.SetVoiceChatActive(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            if (Input.GetKeyDown(input.VoiceChat_KeyCode)) 
            { 
                IsTalking = !IsTalking; // Cập nhật biến mạng } }
            }
        }
    }

    public void OnTalkingChanged()
    {
        if (HasInputAuthority)
        {
            if (LocalHUDController.Instance != null)
            {
                LocalHUDController.Instance.SetVoiceChatActive(IsTalking);
            }
            if (voiceChatUI != null) voiceChatUI.SetActive(false);
        }


        else
        {
            if (voiceChatUI != null) voiceChatUI.SetActive(IsTalking);
        }
    }
}
