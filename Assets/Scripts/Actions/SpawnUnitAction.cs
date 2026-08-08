using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[CreateAssetMenu(menuName = "Actions/SpawnUnit")]
public class SpawnUnitAction : EntityAction
{
    public int unitID = 1;
    public static Action OnSpawn;

    public override bool IsPossible(Entity entity)
    {
        List<Vector3Int> positions = GetValidTargets(entity);
        if (positions != null && positions.Count > 0)
        {
            return true;
        }
        return false;
    }
    
    public void SetSpawnUnit(int unitId)
    {
        unitID = unitId;
    }

    //actually checks to see if the action can be done at position tilePos
    public override bool Action(Entity caster, TileData tileData)
    {
        if (tileData != null && tileData.CanPlaceEntity())
        {
            return true;
        }
        return false;
    }
    //actually preforms the Action on the tile
    public override void PerformAt(Entity caster, TileData tileData)
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        Vector3Int pos = tileData.GetGridPos();

        if (tileData == null)
        {
            return;
        }

        //Make sure the unit entry exists
        UnitInfo info = UnitDatabase.Instance.GetUnitInfo(unitID);
        gameManager.SpawnUnitOnTile(info, pos);
        //Deactivate that unit
        tileData.GetOccupyingEntity().Deactivate();
        OnSpawn?.Invoke();
    }
}
