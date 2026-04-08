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
    public bool ZoomMode = false;       //Block Run
    public bool hasZoomImg = false;
    public float DivmouseSensitivity = 3;
    public Vector3 cameraholder = new Vector3(0.4f, 0.05f, 0f);
    public float DistanceCinema = 0.3f;
    public float POV = 25;
    
    [Header("Chế độ bắn")]
    public bool isAutomatic; // True: Đè chuột để bắn, False: Bấm từng phát
    public AmmoType ammoType; // Loại đạn súng này sử dụng
}