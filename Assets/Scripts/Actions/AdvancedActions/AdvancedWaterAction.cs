using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Actions/AdvancedWater")]
public class AdvancedWaterAction : BasicWaterAction
{

    //actually preforms the Action on the tile
    public override void PerformAt(Entity caster, TileData tileData)
    {
        TileManager manager = FindFirstObjectByType<TileManager>();
        Entity entity = tileData.occupyingEntity;

        //make sure an entity exists
        if(entity == null)
        {
            return;
        }

        Resource crop = entity as Resource;
        Unit unit = entity as Unit;

        if(crop != null)
        {
            crop.Interact();
        }
        else if(unit != null)
        {
            AddDebuff(unit);
        }

        manager.SetTile(tileData.GetGridPos(), TileType.WateredDirt);
        onWater?.Invoke();
    }

    public void AddDebuff(Unit targetUnit)
    {
        if(targetUnit != null && targetUnit.GetIsEnemy())
        {
            MovementBuff movementBuff = new MovementBuff(1, -1 * (targetUnit.GetBaseMoveRange() - 1));
            if(movementBuff.baseIncrease > 0)
            {
                movementBuff.baseIncrease = 0;
            }
            targetUnit.AddBuff(movementBuff);
        }
    }
}
