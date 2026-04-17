using UnityEngine;

public enum MedkitType { Syringe, Medkit, Bandage, SmallMedkit, Pillbottle }

[CreateAssetMenu(fileName = "New Medkit Data", menuName = "FPS/Medkit Data")]
public class MedkitData : ScriptableObject
{
    [Header("Thông tin hiển thị")]
    public string MedkitName;
    public Sprite MedkitIcon;
    public GameObject MedkitPrefab;

     [Header("Thông số Medkit")]
     public float timeToUse = 5f;
     public float healAmount = 50f; 
}
