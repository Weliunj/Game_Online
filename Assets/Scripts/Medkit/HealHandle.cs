using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class HealHandle : NetworkBehaviour
{
    private StatsHandler stats;
    private PlayerCombat combat;
    private PlayerMovement movement;
    private Animator animator;
    private bool _hasConsumedHealingItem;

    [Header("Medkit tool")]
    public int medkitAmount = 0;
    public int bandageAmount = 0;
    public int SmallMedkitAmount = 0;
    public int PillbottleAmount = 0;
    public int SyringeAmount = 0;

    [Header("UI")]  //Syringe 7-80      Medkit 5-40     Small Medkit 3-25   Bandage 2-15    PillBottle 1-10
    public bool toggleHeal;
    private const int HealTypeCount = 5;
    private GameObject HealPanel;
    private Button[] useBtn = new Button[HealTypeCount];
    private Button[] dropBtn = new Button[HealTypeCount];
    public GameObject[] HealWorldPrefab;

    [Header("Medkit Data")]
    public MedkitData[] medkitData;

    [Networked] public bool IsHealing { get; set; }
    [Networked] public int CurrentHealType { get; set; }
    [Networked] public TickTimer HealTimer { get; set; }

    public override void Render()
    {
        if (animator != null)
        {
            animator.SetBool("Healing", IsHealing);
        }

        UpdateHealWorldPrefabs();
    }
    public override void Spawned()
    {
        HealPanel = GameObject.Find("HealingUI");
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<StatsHandler>();
        combat = GetComponent<PlayerCombat>();
        movement = GetComponent<PlayerMovement>();

        if (HealWorldPrefab != null)
        {
            foreach (var prefab in HealWorldPrefab)
            {
                if (prefab != null)
                    prefab.SetActive(false);
            }
        }

        if (HasInputAuthority)
        {
            var hud = LocalHUDController.Instance;
            if (hud != null)
                hud.SetHealHandle(this);
        }

        if (HealPanel != null)
        {
            useBtn[0] = HealPanel.transform.Find("SyringeBtn").GetComponent<Button>();
            useBtn[1] = HealPanel.transform.Find("MedkitBtn").GetComponent<Button>();
            useBtn[2] = HealPanel.transform.Find("SmallMedkitBtn").GetComponent<Button>();
            useBtn[3] = HealPanel.transform.Find("BandageBtn").GetComponent<Button>();
            useBtn[4] = HealPanel.transform.Find("PillbottleBtn").GetComponent<Button>();
            
            dropBtn[0] = HealPanel.transform.Find("DropSyringeBtn").GetComponent<Button>();
            dropBtn[1] = HealPanel.transform.Find("DropMedkitBtn").GetComponent<Button>();
            dropBtn[2] = HealPanel.transform.Find("DropSmallMedkitBtn").GetComponent<Button>();
            dropBtn[3] = HealPanel.transform.Find("DropBandageBtn").GetComponent<Button>();
            dropBtn[4] = HealPanel.transform.Find("DropPillbottleBtn").GetComponent<Button>();
            HealPanel.SetActive(false);
        }
        if (HasInputAuthority)
        {
            if (useBtn[0] != null) useBtn[0].onClick.AddListener(() => medkitUse(MedkitType.Syringe));
            if (useBtn[1] != null) useBtn[1].onClick.AddListener(() => medkitUse(MedkitType.Medkit));
            if (useBtn[2] != null) useBtn[2].onClick.AddListener(() => medkitUse(MedkitType.SmallMedkit));
            if (useBtn[3] != null) useBtn[3].onClick.AddListener(() => medkitUse(MedkitType.Bandage));
            if (useBtn[4] != null) useBtn[4].onClick.AddListener(() => medkitUse(MedkitType.Pillbottle));

            if (dropBtn[0] != null) dropBtn[0].onClick.AddListener(() => DropMedkit(MedkitType.Syringe));
            if (dropBtn[1] != null) dropBtn[1].onClick.AddListener(() => DropMedkit(MedkitType.Medkit));
            if (dropBtn[2] != null) dropBtn[2].onClick.AddListener(() => DropMedkit(MedkitType.SmallMedkit));
            if (dropBtn[3] != null) dropBtn[3].onClick.AddListener(() => DropMedkit(MedkitType.Bandage));
            if (dropBtn[4] != null) dropBtn[4].onClick.AddListener(() => DropMedkit(MedkitType.Pillbottle));
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (stats == null) stats = GetComponent<StatsHandler>();
        if (stats == null) return;

        if (stats.IsDead && IsHealing)
        {
            CancelHealingInternal(true);
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
            SetHealPanelActive(!toggleHeal);
        }

        if (IsHealing && ShouldCancelHealing())
        {
            RPC_CancelHeal();
        }
    }

    private bool ShouldCancelHealing()
    {
        if (combat != null && (combat.IsShooting || combat.IsZooming || combat.IsReloading)) return true;
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
        if (IsHealing) return;
        if (!toggleHeal) return;
        SetHealPanelActive(false);
        RPC_RequestHeal((int)type);
    }

    public void DropMedkit(MedkitType type)
    {
        if (!HasInputAuthority) return;
        if (!toggleHeal) return;
        RPC_RequestDropMedkit((int)type);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestDropMedkit(int type)
    {
        if (!Object.HasStateAuthority) return;
        if (!System.Enum.IsDefined(typeof(MedkitType), type)) return;

        MedkitType medkitType = (MedkitType)type;
        if (!HasMedkit(medkitType)) return;

        if (IsHealing)
        {
            CancelHealingInternal(true);
        }

        MedkitData data = GetMedkitData(medkitType);
        if (data == null || data.MedkitPrefab == null) return;

        DeductMedkit(medkitType);

        var dropPosition = combat != null && combat.DropPos != null ? combat.DropPos.transform.position : transform.position + transform.forward;
        var dropRotation = combat != null && combat.DropPos != null ? combat.DropPos.transform.rotation : transform.rotation;

        var droppedObject = Runner.Spawn(data.MedkitPrefab, dropPosition, dropRotation);
        if (droppedObject != null)
        {
            var droppedGameObject = droppedObject.gameObject;
            Rigidbody rb = droppedGameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
            }
        }
    }

    private void SetHealPanelActive(bool active)
    {
        toggleHeal = active;
        if (HealPanel != null)
            HealPanel.SetActive(active);

        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestHeal(int type)
    {
        if (!Object.HasStateAuthority) return;
        if (!System.Enum.IsDefined(typeof(MedkitType), type)) return;

        if (stats == null) stats = GetComponent<StatsHandler>();
        if (stats == null || stats.IsDead || IsHealing) return;

        MedkitType medkitType = (MedkitType)type;
        if (!HasMedkit(medkitType)) return;

        MedkitData data = GetMedkitData(medkitType);
        if (data == null) return;

        DeductMedkit(medkitType);
        _hasConsumedHealingItem = true;
        IsHealing = true;
        CurrentHealType = type;
        HealTimer = TickTimer.CreateFromSeconds(Runner, data.timeToUse);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_CancelHeal()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsHealing) return;
        CancelHealingInternal(true);
    }

    private void CancelHealingInternal(bool refund)
    {
        if (refund && _hasConsumedHealingItem && System.Enum.IsDefined(typeof(MedkitType), CurrentHealType))
        {
            RefundMedkit((MedkitType)CurrentHealType);
        }

        _hasConsumedHealingItem = false;
        IsHealing = false;
        CurrentHealType = -1;
        HealTimer = TickTimer.None;
        UpdateHealWorldPrefabs();
    }

    private void CompleteHealing()
    {
        MedkitData data = GetMedkitData((MedkitType)CurrentHealType);
        if (data != null && stats != null)
        {
            stats.NetworkHealth = Mathf.Min(stats.maxHealth, stats.NetworkHealth + data.healAmount);
        }

        CancelHealingInternal(false);
    }

    private void UpdateHealWorldPrefabs()
    {
        if (HealWorldPrefab == null || HealWorldPrefab.Length == 0)
            return;

        int activeIndex = -1;
        if (IsHealing && System.Enum.IsDefined(typeof(MedkitType), CurrentHealType))
        {
            activeIndex = GetHealPrefabIndex((MedkitType)CurrentHealType);
        }

        for (int i = 0; i < HealWorldPrefab.Length; i++)
        {
            if (HealWorldPrefab[i] != null)
                HealWorldPrefab[i].SetActive(i == activeIndex);
        }
    }

    private int GetHealPrefabIndex(MedkitType type)
    {
        return type switch
        {
            MedkitType.Syringe => 0,
            MedkitType.Medkit => 1,
            MedkitType.SmallMedkit => 2,
            MedkitType.Bandage => 3,
            MedkitType.Pillbottle => 4,
            _ => -1,
        };
    }

    private bool HasMedkit(MedkitType type)
    {
        return type switch
        {
            MedkitType.Bandage => bandageAmount > 0,
            MedkitType.SmallMedkit => SmallMedkitAmount > 0,
            MedkitType.Medkit => medkitAmount > 0,
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
            case MedkitType.SmallMedkit: SmallMedkitAmount = Mathf.Max(0, SmallMedkitAmount - 1); break;
            case MedkitType.Medkit: medkitAmount = Mathf.Max(0, medkitAmount - 1); break;
            case MedkitType.Pillbottle: PillbottleAmount = Mathf.Max(0, PillbottleAmount - 1); break;
            case MedkitType.Syringe: SyringeAmount = Mathf.Max(0, SyringeAmount - 1); break;
        }
    }

    private void RefundMedkit(MedkitType type)
    {
        switch (type)
        {
            case MedkitType.Bandage: bandageAmount += 1; break;
            case MedkitType.SmallMedkit: SmallMedkitAmount += 1; break;
            case MedkitType.Medkit: medkitAmount += 1; break;
            case MedkitType.Pillbottle: PillbottleAmount += 1; break;
            case MedkitType.Syringe: SyringeAmount += 1; break;
        }
    }

    private MedkitData GetMedkitData(MedkitType type)
    {
        int index = GetMedkitIndex(type);
        if (index < 0 || medkitData == null || index >= medkitData.Length)
            return null;

        return medkitData[index];
    }

    // Order must match the inspector order for medkitData and HealWorldPrefab.
    private int GetMedkitIndex(MedkitType type)
    {
        return type switch
        {
            MedkitType.Syringe => 0,
            MedkitType.Medkit => 1,
            MedkitType.SmallMedkit => 2,
            MedkitType.Bandage => 3,
            MedkitType.Pillbottle => 4,
            _ => -1,
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
            case MedkitType.Syringe: SyringeAmount += amount; break;
            case MedkitType.Medkit: medkitAmount += amount; break;
            case MedkitType.SmallMedkit: SmallMedkitAmount += amount; break;
            case MedkitType.Bandage: bandageAmount += amount; break;
            case MedkitType.Pillbottle: PillbottleAmount += amount; break;
            
        }
    }

}

