using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
public partial class LocalHUDController : MonoBehaviour
{
    [Header("---------------HealHandle---------------")]
    public MedkitData[] medkitData;
    private HealHandle healHandle;

    [Header("Text")]
    public TextMeshProUGUI[] HealText;
    public TextMeshProUGUI[] infMedkitText;
    public GameObject[] dropBtn;

    private void Heal_Awake()
    {
        foreach(var h in HealText){ h.text = "0"; }
        for (int i = 0; i < infMedkitText.Length; i++)
        {
            infMedkitText[i].text = $"Heal: <color=green>{Convert.ToString(medkitData[i].healAmount)}</color>\nTime: <color=green>{Convert.ToString(medkitData[i].timeToUse)}s</color>";
        }

        foreach(var h in dropBtn){ h.SetActive(false);}
    }

    private void Heal_Update()
    {
        if(healHandle == null) return;
        
        if(healHandle.toggleHeal) 
        {
            UpdateBtn();
            UpdateMedkitUI();
        }
    }
    
    public void UpdateBtn()
    {
        if(dropBtn == null) return;
        dropBtn[0].SetActive(healHandle.SyringeAmount > 0);
        dropBtn[1].SetActive(healHandle.medkitAmount > 0);
        dropBtn[2].SetActive(healHandle.SmallMedkitAmount > 0);
        dropBtn[3].SetActive(healHandle.bandageAmount > 0);
        dropBtn[4].SetActive(healHandle.PillbottleAmount > 0);
    }
    private void UpdateMedkitUI()
    {
        SetText(HealText[0], Convert.ToString(healHandle.SyringeAmount));
        SetText(HealText[1], Convert.ToString(healHandle.medkitAmount));
        SetText(HealText[2], Convert.ToString(healHandle.SmallMedkitAmount));
        SetText(HealText[3], Convert.ToString(healHandle.bandageAmount));
        SetText(HealText[4], Convert.ToString(healHandle.PillbottleAmount));
    }
}
