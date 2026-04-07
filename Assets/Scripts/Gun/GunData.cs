using UnityEngine;

public enum AmmoType { Pistol, Rifle, Sniper }

[CreateAssetMenu(fileName = "New Weapon", menuName = "FPS/Weapon Data")]
public class GunData : ScriptableObject 
{
    [Header("Thông tin hiển thị")]
    public string weaponName;
    public Sprite weaponIcon;

    [Header("Thông số chiến đấu")]
    public float damage = 15f;
    public float fireRate = 2f;    // Thời gian giữa 2 lần bắn
    public float range = 150f;       // Tầm bắn
    public int magSize = 15;         // Băng đạn
    public float reloadTime = 2f;    // Thời gian thay đạn

    [Header("ZoomMode")]     // 0.28 0.05/ o.3   /Setting pov1: distan 0, hodler 0, 0.2     // Nham : Len= 50, setivi / 3
    public bool ZoomMode = false;
    public GameObject ZoomImg = null;
    public GameObject[] PlayerMesh;
    public Transform cameraholder;
    public float DistanceCinema;
    public float mouseSensitivity;
    public float LenCinema;
    
    [Header("Chế độ bắn")]
    public bool isAutomatic; // True: Đè chuột để bắn, False: Bấm từng phát
    public AmmoType ammoType; // Loại đạn súng này sử dụng
}