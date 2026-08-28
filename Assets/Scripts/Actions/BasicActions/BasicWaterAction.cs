using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Actions/Water")]
public class BasicWaterAction : EntityAction
{
    public static Action onWater;

    //actually checks to see if the action can be done at position tilePos
    public override bool Action(Entity caster, TileData tileData)
    {
        if (tileData != null && tileData.HasOccupant())
        {
            Resource cropCheck = tileData.occupyingEntity as Resource;
            //You only need to water crops if they aren't fully grown and they haven't been watered already
            if (cropCheck != null && (!cropCheck.CanInteract() && !cropCheck.IsHarvestable()))
            {
                
                return true;
            }

        }
        return false;
    }

    //actually preforms the Action on the tile
    public override void PerformAt(Entity caster, TileData tileData)
    {
        Resource targetCrop = tileData.occupyingEntity as Resource;
        TileManager manager = FindFirstObjectByType<TileManager>();

        //make sure a crop exists
        if (targetCrop == null)
        {
            Debug.Log("No Crop");
            return;
        }

        manager.SetTile(tileData.GetGridPos(), TileType.WateredDirt);
        targetCrop.Interact();
        onWater?.Invoke();
    }
}
