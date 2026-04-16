using UnityEngine;

public enum MedkitType { Bandage = 0, MedkitS = 1, MedkitL = 2, Pillbottle = 3, Syringe = 4 }

[CreateAssetMenu(fileName = "New Medkit Data", menuName = "FPS/Medkit Data")]
public class MedkitData : ScriptableObject
{
    [Header("Thông tin hiển thị")]
    public string MedkitName;
    public Sprite MedkitIcon;

     [Header("Thông số Medkit")]
     public float timeToUse = 5f;
     public float healAmount = 50f; 
}
