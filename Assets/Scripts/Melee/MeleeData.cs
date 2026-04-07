using UnityEngine;

[CreateAssetMenu(fileName = "New Melee", menuName = "FPS/Melee Data")] // Thêm dòng này
public class MeleeData : ScriptableObject
{
    [Header("Thông tin hiển thị")]
    public string WeaponName;
    public Sprite WeaponIcon; 

    [Header("Thông số chiến đấu")]
    public float damage = 5f;
    public float staminaCost = 10f;
    public float attackRate = 0.5f;
}