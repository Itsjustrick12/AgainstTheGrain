using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "NewResource", menuName = "AgainstTheGrain/Entities/Resource")]
public class ResourceInfo : EntityInfo
{
    [Header("Resource Specific")]
    //base stage counts as a stage, if you want a simple "grow for one turn to harvest" this number would be two
    public int numStages;
    //Used to progress to full harvest, these are the sprites rendered on the tilemap
    //There should be a sprite for each sprite
    public Sprite[] growthStageSprites;


    [Header("MultiHarvesting")]
    public bool renewable = false;
    public int onHarvestStage = -1;
    public Sprite barrenSprite = null;
}