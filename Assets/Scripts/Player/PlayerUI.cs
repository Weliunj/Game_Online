using UnityEngine;
using Fusion;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : NetworkBehaviour
{
    private StatsHandler stats;

    [Header("World Space UI")]
    public GameObject worldCanvas; 
    public Slider worldHPSlider;
    [SerializeField] private TextMeshProUGUI worldNameText;

    [Networked] public NetworkString<_16> PlayerName { get; set; }

    public override void Spawned()
    {
        stats = GetComponent<StatsHandler>();

        if (Object.HasInputAuthority)
            PlayerName = RoomManager.LocalPlayerName;

        if (Object.HasInputAuthority)
        {
            if (worldCanvas != null) worldCanvas.SetActive(false);
        }
        else if (worldCanvas != null) 
        {
            worldCanvas.SetActive(true);
        }
    }
    public override void Render()
    {
        float hpPercent = stats.NetworkHealth / stats.maxHealth;

        if (worldHPSlider != null) worldHPSlider.value = hpPercent;
        if (worldNameText != null)
        {
            worldNameText.text = PlayerName.ToString();
            worldNameText.gameObject.SetActive(!Object.HasInputAuthority);
        }
    }
}