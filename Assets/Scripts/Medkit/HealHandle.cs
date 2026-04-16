using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class HealHandle : NetworkBehaviour
{
    private bool toggleHeal = false;

    private GameObject HealPanel;
    private Button bandageBtn;
    private Button medkitSBtn;
    private Button medkitLBtn;
    private Button PillbottleBtn;
    private Button SyringeBtn;

    [Header("Medkit tool")]
    public int medkitLAmount = 0;
    public int bandageAmount = 0;
    public int medkitSAmount = 0;
    public int PillbottleAmount = 0;
    public int SyringeAmount = 0;

    [Header("Medkit Data")]
    public MedkitData bandageData;
    public MedkitData medkitSData;
    public MedkitData medkitLData;
    public MedkitData PillbottleData;
    public MedkitData SyringeData;

    private StatsHandler stats;
    private PlayerCombat combat;
    private PlayerMovement movement;
    private Animator animator;

    [Networked] public bool IsHealing { get; set; }
    [Networked] public int CurrentHealType { get; set; }
    [Networked] public TickTimer HealTimer { get; set; }

    public override void Render()
    {
        if (animator != null)
        {
            animator.SetBool("Healing", IsHealing);
        }
    }
    public override void Spawned()
    {
        HealPanel = GameObject.Find("HealingUI");
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<StatsHandler>();
        combat = GetComponent<PlayerCombat>();
        movement = GetComponent<PlayerMovement>();

        if (HealPanel != null)
        {
            bandageBtn = HealPanel.transform.Find("BandageBtn").GetComponent<Button>();
            medkitSBtn = HealPanel.transform.Find("MedkitSBtn").GetComponent<Button>();
            medkitLBtn = HealPanel.transform.Find("MedkitLBtn").GetComponent<Button>();
            PillbottleBtn = HealPanel.transform.Find("PillbottleBtn").GetComponent<Button>();
            SyringeBtn = HealPanel.transform.Find("SyringeBtn").GetComponent<Button>();
            HealPanel.SetActive(false);
        }

        if (HasInputAuthority)
        {
            if (bandageBtn != null) bandageBtn.onClick.AddListener(() => medkitUse(MedkitType.Bandage));
            if (medkitSBtn != null) medkitSBtn.onClick.AddListener(() => medkitUse(MedkitType.MedkitS));
            if (medkitLBtn != null) medkitLBtn.onClick.AddListener(() => medkitUse(MedkitType.MedkitL));
            if (PillbottleBtn != null) PillbottleBtn.onClick.AddListener(() => medkitUse(MedkitType.Pillbottle));
            if (SyringeBtn != null) SyringeBtn.onClick.AddListener(() => medkitUse(MedkitType.Syringe));
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (stats == null) stats = GetComponent<StatsHandler>();
        if (stats == null) return;

        if (stats.IsDead && IsHealing)
        {
            CancelHealingInternal();
            return;
        }

        if (IsHealing && HealTimer.Expired(Runner))
        {
            CompleteHealing();
        }
    }

    public void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.H))
        {
            toggleHeal = !toggleHeal;
            Cursor.lockState = toggleHeal ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = toggleHeal;
            UpdateHealPanel();
        }

        if (stats != null && stats.IsHealing && ShouldCancelHealing())
        {
            RPC_CancelHeal();
        }
    }

    private void UpdateHealPanel()
    {
        if (HealPanel != null)
            HealPanel.SetActive(toggleHeal);
    }

    private bool ShouldCancelHealing()
    {
        if (combat != null && combat.IsShooting) return true;
        if (movement != null && movement.isSprinting) return true;
        if (stats != null && stats.IsDancing) return true;
        if (Input.GetButtonDown("Jump")) return true;
        if (Input.GetKeyDown(KeyCode.Z)) return true;
        if (Input.GetMouseButtonDown(0)) return true;
        return false;
    }

    public void medkitUse(MedkitType type)
    {
        if (!HasInputAuthority) return;
        if (!toggleHeal) return;

        RPC_RequestHeal((int)type);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestHeal(int type)
    {
        if (!Object.HasStateAuthority) return;
        if (!System.Enum.IsDefined(typeof(MedkitType), type)) return;
        if (stats == null) stats = GetComponent<StatsHandler>();
        if (stats == null || stats.IsDead || IsHealing) return;
        if (stats.NetworkHealth >= stats.maxHealth) return;

        MedkitType medkitType = (MedkitType)type;
        if (!HasMedkit(medkitType)) return;

        MedkitData data = GetMedkitData(medkitType);
        if (data == null) return;

        DeductMedkit(medkitType);
        IsHealing = true;
        CurrentHealType = type;
        HealTimer = TickTimer.CreateFromSeconds(Runner, data.timeToUse);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_CancelHeal()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsHealing) return;
        CancelHealingInternal();
    }

    private void CancelHealingInternal()
    {
        IsHealing = false;
        CurrentHealType = -1;
        HealTimer = TickTimer.None;
    }

    private void CompleteHealing()
    {
        MedkitData data = GetMedkitData((MedkitType)CurrentHealType);
        if (data != null && stats != null)
        {
            stats.NetworkHealth = Mathf.Min(stats.maxHealth, stats.NetworkHealth + data.healAmount);
        }

        CancelHealingInternal();
    }

    private bool HasMedkit(MedkitType type)
    {
        return type switch
        {
            MedkitType.Bandage => bandageAmount > 0,
            MedkitType.MedkitS => medkitSAmount > 0,
            MedkitType.MedkitL => medkitLAmount > 0,
            MedkitType.Pillbottle => PillbottleAmount > 0,
            MedkitType.Syringe => SyringeAmount > 0,
            _ => false,
        };
    }

    private void DeductMedkit(MedkitType type)
    {
        switch (type)
        {
            case MedkitType.Bandage: bandageAmount = Mathf.Max(0, bandageAmount - 1); break;
            case MedkitType.MedkitS: medkitSAmount = Mathf.Max(0, medkitSAmount - 1); break;
            case MedkitType.MedkitL: medkitLAmount = Mathf.Max(0, medkitLAmount - 1); break;
            case MedkitType.Pillbottle: PillbottleAmount = Mathf.Max(0, PillbottleAmount - 1); break;
            case MedkitType.Syringe: SyringeAmount = Mathf.Max(0, SyringeAmount - 1); break;
        }
    }

    private MedkitData GetMedkitData(MedkitType type)
    {
        return type switch
        {
            MedkitType.Bandage => bandageData,
            MedkitType.MedkitS => medkitSData,
            MedkitType.MedkitL => medkitLData,
            MedkitType.Pillbottle => PillbottleData,
            MedkitType.Syringe => SyringeData,
            _ => null,
        };
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickUpMedkit(int amount, int type)
    {
        if (!Object.HasStateAuthority) return;
        if (!System.Enum.IsDefined(typeof(MedkitType), type))
            return;

        MedkitType medkitType = (MedkitType)type;
        switch (medkitType)
        {
            case MedkitType.Bandage: bandageAmount += amount; break;
            case MedkitType.MedkitS: medkitSAmount += amount; break;
            case MedkitType.MedkitL: medkitLAmount += amount; break;
            case MedkitType.Pillbottle: PillbottleAmount += amount; break;
            case MedkitType.Syringe: SyringeAmount += amount; break;
        }
    }
}
