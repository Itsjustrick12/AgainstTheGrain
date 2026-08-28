using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Actions/AdvancedPlant")]
public class AdvancedPlantAction : PlantAction
{

    //actually preforms the Action on the tile
    public override void PerformAt(Entity caster, TileData tileData)
    {
        //attempt to plant crop
        Vector3Int pos = tileData.GetGridPos();
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.SpawnResourceOnTile(ResourceDatabase.Instance.GetResourceInfo(cropID), pos);
        onPlant?.Invoke();
    }
}
