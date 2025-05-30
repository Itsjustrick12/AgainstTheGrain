using UnityEngine;

[CreateAssetMenu(menuName ="My Assets/ Crop Obj")]
public class Crop : ScriptableObject
{
    public int numStages = 0;
    public Sprite[] sprites;
    public int[] growthDays;
    public string cropName = "New Crop";
}
