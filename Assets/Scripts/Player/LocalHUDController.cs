using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Fusion;

public class LocalHUDController : MonoBehaviour
{
    public static LocalHUDController Instance { get; private set; }

    [Header("Local HUD Sliders")]
    [SerializeField] private Slider localHPSlider;
    [SerializeField] private Slider localStaminaSlider;

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

    public TextMeshProUGUI GunNameText;
    public TextMeshProUGUI GunAmmoText;
    public TextMeshProUGUI GunDamageText;
    public TextMeshProUGUI GunFireRateText;
    public TextMeshProUGUI GunRangeText;
    public TextMeshProUGUI GunMagSizeText;
    public TextMeshProUGUI GunReloadTimeText;
    public TextMeshProUGUI GunisAutomaticText;
    
    public TextMeshProUGUI PistolAmmoText;
    public TextMeshProUGUI RifleAmmoText;
    public TextMeshProUGUI SniperAmmoText;

    private StatsHandler _target;
    private float _fpsSmoothed;

    private void Awake()
    {
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

    public void SetTarget(StatsHandler target)
    {
        _target = target;
    }

    private void Update()
    {
        // Kiểm tra nếu phím P được nhấn
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleDebugUI();
        }

        // Chỉ cập nhật thông số khi DebugUI đang hiện để tiết kiệm hiệu năng
        if (DebugUI != null && DebugUI.activeSelf)
        {
            UpdateDebugUI();
        }
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

        if (_target == null)
        {
            SetText(PingText, "Ping: N/A");
            SetText(ServerStatusText, "Status: N/A");
            SetText(ServerText, "Server: N/A");
            SetText(PlayerNameText, "Player: N/A");
            SetText(PlayerHealthText, "HP: N/A");
            SetText(PlayerStaminaText, "Stamina: N/A");
            SetText(PlayerisSprintingText, "Sprinting: N/A");
            SetText(PlayerisCrouchingText, "Crouching: N/A");
            SetText(PistolAmmoText, "PistolReserve: -");
            SetText(RifleAmmoText, "RifleReserve: -");
            SetText(SniperAmmoText, "SniperReserve: -");
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

        var runner = _target.Runner;
        SetText(PingText, "Ping: N/A");
        SetText(ServerStatusText, runner != null && runner.IsRunning ? $"Status: {runner.Mode}" : "Status: Offline");
        SetText(ServerText, runner != null && runner.IsRunning ? "Server: Connected" : "Server: Disconnected");

        var pui = _target.GetComponent<PlayerUI>();
        SetText(PlayerNameText, pui != null ? $"Player: {pui.PlayerName}" : "Player: N/A");
        SetText(PlayerHealthText, $"HP: {Mathf.CeilToInt(_target.NetworkHealth)} / {Mathf.CeilToInt(_target.maxHealth)}");
        SetText(PlayerStaminaText, $"Stamina: {Mathf.CeilToInt(_target.NetworkStamina)} / {Mathf.CeilToInt(_target.maxStamina)}");

        var movement = _target.GetComponent<PlayerMovement>();
        SetText(PlayerisSprintingText, $"Sprinting: {(movement != null && movement.isSprinting ? "Yes" : "No")}");
        SetText(PlayerisCrouchingText, $"Crouching: {(_target.IsCrouching ? "Yes" : "No")}");

        var combat = _target.GetComponent<PlayerCombat>();
        SetText(PistolAmmoText, combat != null ? $"PistolReserve: {combat.pistolAmmoReserve}" : "PistolReserve: -");
        SetText(RifleAmmoText, combat != null ? $"RifleReserve: {combat.rifleAmmoReserve}" : "RifleReserve: -");
        SetText(SniperAmmoText, combat != null ? $"SniperReserve: {combat.sniperAmmoReserve}" : "SniperReserve: -");
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
        SetText(GunAmmoText, $"Ammo: {gun.ammoRemaining} / {data.magSize}");
        SetText(GunDamageText, $"Damage: {data.damage:0.##}");
        SetText(GunFireRateText, $"FireRate: {data.fireRate:0.###}");
        SetText(GunRangeText, $"Range: {data.range:0.##}");
        SetText(GunMagSizeText, $"Mag: {data.magSize}");
        SetText(GunReloadTimeText, $"Reload: {data.reloadTime:0.##}s");
        SetText(GunisAutomaticText, $"Auto: {(data.isAutomatic ? "Yes" : "No")}");
    }

    private static void SetText(TextMeshProUGUI field, string value)
    {
        if (field != null) field.text = value;
    }
}
