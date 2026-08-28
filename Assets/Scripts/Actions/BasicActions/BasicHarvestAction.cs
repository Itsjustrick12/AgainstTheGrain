using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Actions/Harvest")]
public class BasicHarvestAction : EntityAction
{
    public static Action onHarvest;

    //sees if the action is preformable
    public override bool Action(Entity caster, TileData tileData)
    {
        Resource crop = tileData.GetOccupyingEntity() as Resource;
        if(crop != null && crop.CanBeHarvested())
        {
            return true;
        }
        return false;
    }

    //actually preforms the Action on the tile
    public override void PerformAt(Entity caster, TileData tileData)
    {
        TileManager manager = FindFirstObjectByType<TileManager>();
        Vector3Int pos = tileData.GetGridPos();
        Resource targetCrop = manager.GetResourceOnTile(pos);
        if(targetCrop != null)targetCrop.Harvest(caster.GetTeam());
    }
}
