using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Fusion;
using System;

public class LocalHUDController : MonoBehaviour
{
    public static LocalHUDController Instance { get; private set; }

    [Header("Local HUD Sliders")]
    [SerializeField] private Slider localHPSlider;
    [SerializeField] private Slider localStaminaSlider;
    [SerializeField] private Image Crosshair;

    [Header("Blood Effect")]
    [SerializeField] private Image bloodImage;
    [SerializeField] private float fadeSpeed = 1.5f;

    [Header("Ui")]
    public GameObject DebugUI;
    public TextMeshProUGUI PingText;
    public TextMeshProUGUI ServerStatusText;
    public TextMeshProUGUI FPSText;
    public TextMeshProUGUI ServerText;
    public TextMeshProUGUI PlayerNameText;      // 
    public TextMeshProUGUI PlayerHealthText;
    public TextMeshProUGUI PlayerStaminaText;    // ap dung cho cac bien co the hoi {curr} / {max}
    public TextMeshProUGUI PlayerisSprintingText;
    public TextMeshProUGUI PlayerisCrouchingText;

    public TextMeshProUGUI MeleeNameText;
    public TextMeshProUGUI GunNameText;
    public TextMeshProUGUI GunAmmoText;
    public TextMeshProUGUI GunDamageText;
    public TextMeshProUGUI GunFireRateText;
    public TextMeshProUGUI GunRangeText;
    public TextMeshProUGUI GunMagSizeText;
    public TextMeshProUGUI GunReloadTimeText;
    public TextMeshProUGUI GunReloadingText;
    public TextMeshProUGUI GunisAutomaticText;
    
    public TextMeshProUGUI PistolAmmoText;
    public TextMeshProUGUI RifleAmmoText;
    public TextMeshProUGUI SniperAmmoText;
    public TextMeshProUGUI SmgAmmoText;
    public TextMeshProUGUI ShotgunAmmoText;

    [Header("HealHandle")]
    private HealHandle healHandle;
    public TextMeshProUGUI[] HealText;

    private StatsHandler stats;
    private float _fpsSmoothed;

    private void Awake()
    {
        foreach(var h in HealText){ h.text = "0"; }

        DebugUI.SetActive(false);
        Instance = this;
        if (bloodImage != null)
        {
            var c = bloodImage.color;
            c.a = 0f;
            bloodImage.color = c;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetStatsHandle(StatsHandler _stats)
    {
        stats = _stats;
    }
    public void SetHealHandle(HealHandle _heal)
    {
        healHandle = _heal;
    }

    private void Update()
    {
        // Kiểm tra nếu phím P được nhấn
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleDebugUI();
        }

        UpdateDebugUI();
        if(healHandle.toggleHeal) UpdateMedkitUI();
    }

    private void ToggleDebugUI()
    {
        if (DebugUI != null)
        {
            // Đảo ngược trạng thái active hiện tại (true -> false, false -> true)
            DebugUI.SetActive(!DebugUI.activeSelf);
        }
    }

    public void SetBars(float hpPercent, float staminaPercent)
    {
        if (localHPSlider != null) localHPSlider.value = hpPercent;
        if (localStaminaSlider != null) localStaminaSlider.value = staminaPercent;
    }

    public void TriggerBloodEffect()
    {
        if (bloodImage == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeBloodScreen());
    }

    public void SetPermanentBloodEffect()
    {
        if (bloodImage == null) return;
        StopAllCoroutines();
        var c = bloodImage.color;
        c.a = 0.8f;
        bloodImage.color = c;
    }

    private IEnumerator FadeBloodScreen()
    {
        if (bloodImage == null) yield break;

        var c = bloodImage.color;
        c.a = 1f;
        bloodImage.color = c;

        while (bloodImage.color.a > 0f)
        {
            c.a -= Time.deltaTime * fadeSpeed;
            bloodImage.color = c;
            yield return null;
        }
    }

    private void UpdateDebugUI()
    {
        _fpsSmoothed = Mathf.Lerp(_fpsSmoothed, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);
        SetText(FPSText, $"FPS: {Mathf.RoundToInt(_fpsSmoothed)}");

        if (stats == null)
        {
            SetText(PingText, "Ping: N/A");
            SetText(ServerStatusText, "Status: N/A");
            SetText(ServerText, "Server: N/A");
            SetText(PlayerNameText, "Player: N/A");
            SetText(PlayerHealthText, "HP: N/A");
            SetText(PlayerStaminaText, "Stamina: N/A");
            SetText(PlayerisSprintingText, "Sprinting: N/A");
            SetText(PlayerisCrouchingText, "Crouching: N/A");
            SetText(PistolAmmoText, "PistolAmmo: -");
            SetText(RifleAmmoText, "RifleAmmo: -");
            SetText(SniperAmmoText, "SniperAmmo: -");
            SetText(SmgAmmoText, "SmgAmmo: -");
            SetText(ShotgunAmmoText, "ShotgunAmmo: -");
            SetText(MeleeNameText, "Melee: None");
            if (MeleeNameText != null) MeleeNameText.color = Color.white;
            
            SetText(GunNameText, "Gun: None");
            if (GunNameText != null) GunNameText.color = Color.white;
            SetText(GunAmmoText, "Ammo: -");
            SetText(GunDamageText, "Damage: -");
            SetText(GunFireRateText, "FireRate: -");
            SetText(GunRangeText, "Range: -");
            SetText(GunMagSizeText, "Mag: -");
            SetText(GunReloadTimeText, "Reload: -");
            SetText(GunReloadingText, "");
            SetText(GunisAutomaticText, "Auto: -");
            return;
        }

        var runner = stats.Runner;
        SetText(PingText, "Ping: N/A");
        SetText(ServerStatusText, runner != null && runner.IsRunning ? $"Status: {runner.Mode}" : "Status: Offline");
        SetText(ServerText, runner != null && runner.IsRunning ? "Server: Connected" : "Server: Disconnected");

        var pui = stats.GetComponent<PlayerUI>();
        SetText(PlayerNameText, pui != null ? $"Player: {pui.PlayerName}" : "Player: N/A");
        SetText(PlayerHealthText, $"HP: {Mathf.CeilToInt(stats.NetworkHealth)} / {Mathf.CeilToInt(stats.maxHealth)}");
        SetText(PlayerStaminaText, $"Stamina: {Mathf.CeilToInt(stats.NetworkStamina)} / {Mathf.CeilToInt(stats.maxStamina)}");

        var movement = stats.GetComponent<PlayerMovement>();
        SetText(PlayerisSprintingText, $"Sprinting: {(movement != null && movement.isSprinting ? "Yes" : "No")}");
        SetText(PlayerisCrouchingText, $"Crouching: {(stats.IsCrouching ? "Yes" : "No")}");

        var combat = stats.GetComponent<PlayerCombat>();
        SetText(PistolAmmoText, combat != null ? $"PistolAmmo: {combat.pistolAmmoReserve}" : "PistolAmmo: -");
        SetText(RifleAmmoText, combat != null ? $"RifleAmmo: {combat.rifleAmmoReserve}" : "RifleAmmo: -");
        SetText(SniperAmmoText, combat != null ? $"SniperAmmo: {combat.sniperAmmoReserve}" : "SniperAmmo: -");
        SetText(SmgAmmoText, combat != null ? $"SmgAmmo: {combat.smgAmmoReserve}" : "SmgAmmo: -");
        SetText(ShotgunAmmoText, combat != null ? $"ShotgunAmmo: {combat.shotgunAmmoReserve}" : "ShotgunAmmo: -");

        if (combat != null)
        {
            // Lấy tên vũ khí cận chiến hoặc báo Tay không
            string meleeName = combat.meleeInSlot != null ? combat.meleeInSlot.WeaponName : (combat.FistInSlot != null ? combat.FistInSlot.WeaponName : "Fist");
            SetText(MeleeNameText, $"Melee: {meleeName}");
            
            // Đổi màu text thành màu xanh lá nếu curSlot tương ứng được kích hoạt
            if (MeleeNameText != null) MeleeNameText.color = combat.curSlot == 1 ? Color.green : Color.white;
            if (GunNameText != null) GunNameText.color = combat.curSlot == 2 ? Color.green : Color.white;
        }

        if (combat.equippedGun != null && combat.IsReloading)
        {
            SetText(GunReloadingText, "Reloading...");
        }
        else
        {
            SetText(GunReloadingText, "");
        }
        
        var gun = combat != null ? combat.equippedGun : null;
        if (gun == null || gun.gunData == null)
        {
            SetText(GunNameText, "Gun: None");
            SetText(GunAmmoText, "Ammo: -");
            SetText(GunDamageText, "Damage: -");
            SetText(GunFireRateText, "FireRate: -");
            SetText(GunRangeText, "Range: -");
            SetText(GunMagSizeText, "Mag: -");
            SetText(GunReloadTimeText, "Reload: -");
            SetText(GunisAutomaticText, "Auto: -");
            return;
        }

        var data = gun.gunData;
        SetText(GunNameText, $"Gun: {data.weaponName}");
        
        int reserveAmmo = data.ammoType == AmmoType.Pistol ? combat.pistolAmmoReserve 
            : data.ammoType == AmmoType.Rifle ? combat.rifleAmmoReserve 
            : data.ammoType == AmmoType.Sniper ? combat.sniperAmmoReserve
            : data.ammoType == AmmoType.Smg ? combat.smgAmmoReserve
            : combat.shotgunAmmoReserve;
        SetText(GunAmmoText, $"Ammo: {gun.ammoRemaining} / {reserveAmmo}");
        SetText(GunDamageText, $"Damage: {data.damage:0.##}");
        SetText(GunFireRateText, $"FireRate: {data.fireRate:0.###}");
        SetText(GunRangeText, $"Range: {data.range:0.##}");
        SetText(GunMagSizeText, $"Mag: {data.magSize}");
        SetText(GunReloadTimeText, $"Reload: {data.reloadTime:0.##}s");
        SetText(GunisAutomaticText, $"Auto: {(data.isAutomatic ? "Yes" : "No")}");
    }

    private void UpdateMedkitUI()
    {
        SetText(HealText[0], Convert.ToString(healHandle.SyringeAmount));
        SetText(HealText[1], Convert.ToString(healHandle.medkitAmount));
        SetText(HealText[2], Convert.ToString(healHandle.SmallMedkitAmount));
        SetText(HealText[3], Convert.ToString(healHandle.bandageAmount));
        SetText(HealText[4], Convert.ToString(healHandle.PillbottleAmount));
    }
    public void SetCrosshairColor(Color color)
    {
        if (Crosshair != null)
        {
            Crosshair.color = color;
        }
    }

    private static void SetText(TextMeshProUGUI field, string value)
    {
        if (field != null) field.text = value;
    }
}
