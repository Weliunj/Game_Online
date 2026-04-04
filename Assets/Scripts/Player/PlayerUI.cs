using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections;

public class PlayerUI : NetworkBehaviour
{
    private StatsHandler stats;

    [Header("World Space UI")]
    public GameObject worldCanvas; 
    public Slider worldHPSlider;

    [Header("Local HUD Sliders")]
    private Slider localHPSlider;
    private Slider localStaminaSlider;

    [Header("Blood Effect")]
    private Image bloodImage; // Obj BloodScreen trong HUD
    public float fadeSpeed = 1.5f;

    public override void Spawned()
    {
        stats = GetComponent<StatsHandler>();

        if (Object.HasInputAuthority)
        {
            if (worldCanvas != null) worldCanvas.SetActive(false);
            
            GameObject hud = GameObject.FindWithTag("HUD"); 
            if (hud != null)
            {
                localHPSlider = hud.transform.Find("HPSlider").GetComponent<Slider>();
                localStaminaSlider = hud.transform.Find("StaminaSlider").GetComponent<Slider>();
                
                // Tìm BloodScreen nằm trong HUD
                Transform bloodTrans = hud.transform.Find("BloodScreen");
                if (bloodTrans != null)
                {
                    bloodImage = bloodTrans.GetComponent<Image>();
                    // Đặt tàng hình ban đầu
                    Color c = bloodImage.color;
                    c.a = 0;
                    bloodImage.color = c;
                }
            }
        }
        else if (worldCanvas != null) 
        {
            worldCanvas.SetActive(true);
        }
    }

    public void TriggerBloodEffect()
    {
        if (bloodImage != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeBloodScreen());
        }
    }

    private IEnumerator FadeBloodScreen()
    {
        Color tempColor = bloodImage.color;
        tempColor.a = 1f; 
        bloodImage.color = tempColor;

        while (bloodImage.color.a > 0)
        {
            tempColor.a -= Time.deltaTime * fadeSpeed;
            bloodImage.color = tempColor;
            yield return null; 
        }
    }

    public override void Render()
    {
        float hpPercent = stats.NetworkHealth / stats.maxHealth;

        if (worldHPSlider != null) worldHPSlider.value = hpPercent;

        if (Object.HasInputAuthority)
        {
            if (localHPSlider != null) localHPSlider.value = hpPercent;
            if (localStaminaSlider != null) 
                localStaminaSlider.value = stats.NetworkStamina / stats.maxStamina;
        }
    }
}