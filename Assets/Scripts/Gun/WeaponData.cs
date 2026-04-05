using UnityEngine;

// Dòng này giúp bạn chuột phải trong cửa sổ Project để tạo súng mới
[CreateAssetMenu(fileName = "New Weapon", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject 
{
    [Header("Thông tin hiển thị")]
    public string weaponName;
    public Sprite weaponIcon;
    public GameObject weaponPrefab; // Model của súng

    [Header("Thông số chiến đấu")]
    public float damage = 20f;
    public float fireRate = 0.1f;    // Thời gian giữa 2 lần bắn
    public float range = 100f;       // Tầm bắn
    public int magSize = 30;         // Băng đạn
    public float reloadTime = 2f;    // Thời gian thay đạn

    [Header("Hiệu ứng & Âm thanh")]
    public AudioClip shootSound;
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
}