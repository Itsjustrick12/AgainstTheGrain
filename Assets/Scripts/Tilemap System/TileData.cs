using UnityEngine;
public enum TileType
{
    None,
    Grass,
    Dirt,
    WateredDirt,
    //Path,
    //Water
}
public class TileData
{
    public Vector3Int gridPos;
    public TileType type;
    //This is the entity stored at the current tile, could be a crop, unit, structure, or object etc
    public Entity occupyingEntity = null;
    //This is based on the TILE not the entity blocking the tile
    public bool canWalk;
    //Some tiles may require additional movement costs down the line
    //For simple tiles, just set the movement cost to be 1
    public int movementCost;
    //Depending on the type of tile, units may recieve additional protection during combat
    public int defenseBonus;

    //Makes an unoccupied grass tile
    public TileData(Vector3Int pos, TileType type = TileType.Grass, Entity occupyingEntity = null, bool canWalk = true, int movementCost = 1, int defenseBonus = 0)
    {
        gridPos = pos;
        this.type = type;
        this.occupyingEntity = occupyingEntity;
        this.canWalk = canWalk;
        this.movementCost = movementCost;
        this.defenseBonus = defenseBonus;
    }

    public Entity GetOccupyingEntity()
    {
        return occupyingEntity;
    }
    public bool CanEnter()
    {
        return occupyingEntity == null && canWalk;
    }

    //Used for pathfinding algorithms
    public bool IsWalkable()
    {
        return canWalk && (occupyingEntity == null || !occupyingEntity.IsObstacle());
    }
    public bool HasOccupant()
    {
        return occupyingEntity != null;
    }

    public void ClearOccupant()
    {
        occupyingEntity = null;
    }

    public void KillEntity()
    {
        GameObject.Destroy(occupyingEntity.gameObject);
        occupyingEntity = null;
    }

    public void UpdateType(TileType type)
    {
        this.type = type;
        //maybe disable walking if a certain type later

        //attempt to water a crop if theres one here that needs to be watered
        if (type == TileType.WateredDirt &&
            occupyingEntity != null && occupyingEntity is Crop cropCheck)
        {
            cropCheck.WaterCrop();
        }
        //if the type is updated to be grass and theres a crop here, destroy it
        if (type == TileType.Grass && occupyingEntity != null && occupyingEntity is Crop checkCrop){
            checkCrop.DestroyEntity();
            occupyingEntity = null;
        }
    
    }

    public TileType GetType()
    {
        return type;
    }

    public void PlaceEntity(Entity entity)
    {
        if (entity == null)
        {
            Debug.LogError("You are trying to place a NULL entity onto a tile, use clear occupant instead");
            return;
        }

        if (occupyingEntity != null)
        {
            Debug.Log("Entity was overwritten!");
            occupyingEntity.DestroyEntity();
        }

        occupyingEntity = entity;
        occupyingEntity.SetGridPos(gridPos);

        if (!occupyingEntity.GetIsInitialized())
        {
            occupyingEntity.Initialize();
        }

        //Water if the entity is a crop and this tile is watered
        if (occupyingEntity is Crop cropCheck)
        {
            if (type == TileType.WateredDirt)
            {
                cropCheck.WaterCrop();
            }
        }
        //Update fence images
        if (occupyingEntity is Fence fence)
        {
            fence.SetUp();
        }
    }

    public bool CanPlaceEntity()
    {
        if (occupyingEntity == null)
        {
            return true;
        }
        return false;
    }

    public Vector3Int GetGridPos()
    {
        return gridPos;
    }

    public bool HasUnit()
    {
        if (occupyingEntity != null)
        {
            Unit unitCheck = occupyingEntity as Unit;
            if (unitCheck != null)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasEnemyUnit()
    {
        if (occupyingEntity != null)
        {
            Unit unitCheck = occupyingEntity as Unit;
            if (unitCheck != null)
            {
                return unitCheck.GetIsEnemy();
            }
        }
        return false;
    }

    public bool IsPlantable()
    {
        return (occupyingEntity == null) && (type == TileType.Dirt || type == TileType.WateredDirt);
    }

    public void PrintTileData()
    {
        string occupantName = "False";

        if (occupyingEntity != null) {
            occupantName = "True";
        }


        Debug.Log(
            $"--- TILE DATA ---\n" +
            $"Grid Position: {gridPos}\n" +
            $"Type: {type}\n" +
            $"Can Walk: {canWalk}\n" +
            $"Is Walkable(): {IsWalkable()}\n" +
            $"Movement Cost: {movementCost}\n" +
            $"Defense Bonus: {defenseBonus}\n" +
            $"Has Occupant: {HasOccupant()}\n" +
            $"Occupying Entity: {occupantName}\n" +
            $"-------------------"
        );
    }
}
