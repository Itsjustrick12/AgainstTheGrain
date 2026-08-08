using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
//Dual Grid Tilemap Implementation is based on the logic from this video: https://www.youtube.com/watch?v=jEWFSv3ivTg
public class TileManager : MonoBehaviour
{
    //Tilemap the stores the underlying "what kind of tile is here?" information
    //(this should be covered by the display map upon rendering)
    public Tilemap placeholderMap;

    //The display based on the dual grid sprite alignment based on 4 placeholders
    //These two maps are two different layers of the display
    //Displays the underlying tile (grass, dirt, or water)
    public Tilemap baseDisplayMap;
    //Overlay tiles like paths and water are overlayed on top of primary 
    public Tilemap overlayDisplayMap;

    // Holds information about what entities (objects, units, resources) are going to be present at a given tile
    public Tilemap entitiesMap;
    //Will be used for drawing the boarder around where tiles are placed so theres no screen edges
    public Tilemap borderMap;
    public TileBase borderTile;
    public TileHelper tileHelper;

    //Used for getting nearby references of tiles
    [HideInInspector]private static Vector3Int[] NEIGHBORS = new Vector3Int[]
    {
        //Offsets for referencing specific tiles
        new Vector3Int(0,0,0), //Top Right
        new Vector3Int(1,0,0), //Top Left
        new Vector3Int(0,1,0), //Bottom Right
        new Vector3Int(1,1,0) //Bottom Left
    };
    //Used for working backwards from display to placeholder
    public static Vector3Int[] DIRECTIONS = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0)
    };

    //need references to the placeholder tiles to convert to display tiles
    public TileBase grassTile;
    public TileBase waterTile;
    public TileBase dirtTile;
    public TileBase wateredDirtTile;
    public TileBase pathTile;

    //speed of movement (tile per second)f
    public float stepDuration;

    //Dicionary for getting quick references to the data at a given tile (occupants, terrain etc)
    private Dictionary<Vector3Int, TileData> tilePosToData;

    //Stores grass dirt interaction tiles and greyed out for watered soil
    [SerializeField] TileBase[] tiles;

    public void Awake()
    {
        tilePosToData = new Dictionary<Vector3Int, TileData>();
        int size = GameConstants.MapSizeToInt(GameManager.Instance.mapSize);
        for (int i = -size/2; i < size / 2; i++)
        {
            for (int j = -size / 2; j < size / 2; j++)
            {
                Vector3Int tilePos = new Vector3Int(i, j, 0);
                if (!placeholderMap.HasTile(tilePos))
                {
                    SetTile(tilePos, TileType.Grass);
                }
                else
                {
                    SetTile(tilePos, GetTileTypeAt(tilePos));
                }
                    SetBaseDisplayTile(tilePos);
                SetOverlayDisplayTile(tilePos);
            }
        }
        DrawBorder(size);
    }

    //Needed for determining what kind of tile needs to be rendered based on the placeholder
    public TileType GetTileTypeAt(Vector3Int pos)
    {
        return TypeFromPlaceholder(placeholderMap.GetTile(pos));
    }

    //Return the proper placeholder tile based on the passsed type (must check against null!)
    private TileBase PlaceholderFromType(TileType type)
    {
        switch (type)
        {
            case TileType.Grass: return grassTile;
            case TileType.Dirt: return dirtTile;
            case TileType.WateredDirt: return wateredDirtTile;
            //case TileType.Water: return waterTile;
            //case TileType.Path: return pathTile;
            default: return null;
        }
    }
    //Return the proper Type tile based on the passsed placeholder tile
    private TileType TypeFromPlaceholder(TileBase tile)
    {
        if (tile == grassTile) return TileType.Grass;
        else if (tile == dirtTile) return TileType.Dirt;
        else if (tile == wateredDirtTile) return TileType.WateredDirt;
        //else if (tile == waterTile) return TileType.Water;
        //else if (tile == pathTile) return TileType.Path;
        
        //A proper placeholder tile wasn't passed
        return TileType.None;
    }

    //Get a reference to the data stored at any given position
    public TileData GetTileDataAt(Vector3Int pos)
    {
        tilePosToData.TryGetValue(pos, out TileData tileData);
        return tileData;
    }

    //Used when initiating the game scene board
    public void SetTile(Vector3Int pos, TileType type)
    {
        //Update the placeholder map
        placeholderMap.SetTile(pos, PlaceholderFromType(type));

        //Update the 4 affected display tiles
        //If water or path, draw to the overlay

        SetOverlayDisplayTile(pos);


        //Update four surrounding neighbors
        SetBaseDisplayTile(pos);

        //Update Tile Data object to reflect the new type
        if (tilePosToData.ContainsKey(pos))
        {
            //Update the tiledata that exists
            tilePosToData[pos].UpdateType(type);
        }
        else
        {
            tilePosToData[pos] = new TileData(pos, type);
        }

    }

    private void SetBaseDisplayTile(Vector3Int pos)
    {
        //Update the four surrounding tiles (based on neighbors needed for calculations
        for (int i = 0; i < 4; i++)
        {
            Vector3Int tempPosition = pos + NEIGHBORS[i];
            baseDisplayMap.SetTile(tempPosition, CalculateBaseDisplayTile(tempPosition));
        }
    }

    private void SetOverlayDisplayTile(Vector3Int pos)
    {
        //Update the four surrounding tiles (based on neighbors needed for calculations
        for (int i = 0; i < 4; i++)
        {
            Vector3Int tempPosition = pos + NEIGHBORS[i];
            overlayDisplayMap.SetTile(tempPosition, CalculateOverlayDisplayTile(tempPosition));
        }
    }

    //Take the mask value and shift it to be the default layout for the 4x4 tilemap
    //Will save time doing art
    public int ConvertMaskIndex(int mask) {
        
        int[] convert = {
        6,//grass grass grass grass
        5, //TL dirt
        2,//TR dirt
        3,//TL TR dirt
        10,//BL dirt
        1,//TL BL dirt
        4,//TR BL dirt
        13, //TL TR BL dirt
        7, //BR dirt
        14, //TL BR dirt
        11, //TR BR dirt
        0,//TL TR BR dirt
        9, //BL BR dirt
        8, //TL BL BR dirt
        15, //TR BL BR dirt
        12//all dirt
        };

        return convert[mask];
    }

    private TileBase CalculateBaseDisplayTile(Vector3Int pos)
    {
        TileType tR = GetTileTypeAt(pos - NEIGHBORS[0]);
        TileType tL = GetTileTypeAt(pos - NEIGHBORS[1]);
        TileType bR = GetTileTypeAt(pos - NEIGHBORS[2]);
        TileType bL = GetTileTypeAt(pos - NEIGHBORS[3]);

        int mask = 0;

        //Bit math for shifting to the correct tile iteration
        //Requires the sprites to be sorted in an odd way to match the bit logic
        //If marked as dirt of any kind, add the flag for that given corner
        if (tL == TileType.Dirt || tL == TileType.WateredDirt) mask |= 1;   // TL
        if (tR == TileType.Dirt || tR == TileType.WateredDirt) mask |= 2;   // TR
        if (bL == TileType.Dirt || bL == TileType.WateredDirt) mask |= 4;   // BL
        if (bR == TileType.Dirt || bR == TileType.WateredDirt) mask |= 8;   // BR

        return tiles[ConvertMaskIndex(mask)];
    }

    private TileBase CalculateOverlayDisplayTile(Vector3Int pos)
    {
        TileType tR = GetTileTypeAt(pos - NEIGHBORS[0]);
        TileType tL = GetTileTypeAt(pos - NEIGHBORS[1]);
        TileType bR = GetTileTypeAt(pos - NEIGHBORS[2]);
        TileType bL = GetTileTypeAt(pos - NEIGHBORS[3]);

        int mask = 0;

        //Bit math for shifting to the correct tile iteration
        //Requires the sprites to be sorted in an odd way to match the bit logic
        //If watered dirt, add the flag for that given corner
        if (tL == TileType.WateredDirt) mask |= 1;   // TL
        if (tR == TileType.WateredDirt) mask |= 2;   // TR
        if (bL == TileType.WateredDirt) mask |= 4;   // BL
        if (bR == TileType.WateredDirt) mask |= 8;   // BR

        //Return the mask shifted up to the opacity tiles
        return tiles[ConvertMaskIndex(mask) + 16];
    }

    public void PlaceEntityOnTile(Vector3Int pos, Entity entity)
    {
        TileData data = GetTileDataAt(pos);
        if (data == null) return;

        if (data.CanPlaceEntity())
        {
            //Debug.Log("Entity Placed!");
            data.PlaceEntity(entity);
            entity.SetGridPos(pos);
        }
    }

    public void MoveEntity(Vector3Int start, Vector3Int end)
    {
        
        TileData fromData = tilePosToData[start];
        TileData toData = tilePosToData[end];
        if (fromData == null || toData == null)
        {
            return;
        }

        if (toData.CanPlaceEntity() && fromData.HasOccupant())
        {
            //Do swap to new tile
            Entity selectedEntity = fromData.GetOccupyingEntity();
            fromData.ClearOccupant();
            PlaceEntityOnTile(end, selectedEntity);
        }
        
        
    }
    //Need to check for unit null upon return
    public Unit GetUnitOnTile(Vector3Int pos)
    {
        Unit unit = GetEntityOnTile(pos) as Unit;
        return unit;
    }

    public Resource GetResourceOnTile(Vector3Int pos)
    {
        Resource resource = GetEntityOnTile(pos) as Resource;
        return resource;
    }

    public Entity GetEntityOnTile(Vector3Int pos)
    {
        TileData data = GetTileDataAt(pos);
        //try to get a unit
        if (data != null && data.HasOccupant())
        {
            return (data.occupyingEntity);
        }
        return null;
    }

    public void DrawBorder(int size)
    {
        int half = size / 2;
        int borderWidth = 3;

        for (int i = -half - borderWidth; i < half + borderWidth; i++)
        {
            for (int j = -half - borderWidth; j < half + borderWidth; j++)
            {
                // Skip the interior (the actual grid)
                if (i >= -half && i < half && j >= -half && j < half)
                    continue;

                borderMap.SetTile(new Vector3Int(i, j, 0), borderTile);
            }
        }
    }

    private void OnEnable()
    {
        GameManager.StartPlayerTurn += DryWateredTiles;
        Entity.OnEntityDestroyed += RemoveUnit;
    }

    private void OnDisable()
    {
        GameManager.StartPlayerTurn -= DryWateredTiles;
    }

    public void DryWateredTiles()
    {
        List<Vector3Int> wateredPositions = new List<Vector3Int>();

        //Find all watered tiles first
        foreach (var pair in tilePosToData)
        {
            TileData data = pair.Value;
            if (data.type == TileType.WateredDirt)
            {
                wateredPositions.Add(pair.Key);
            }
        }

        //Update the necessary tiles
        foreach (var pos in wateredPositions)
        {
            // Update the TileData type
            tilePosToData[pos].UpdateType(TileType.Dirt);

            // Update the placeholder map & visuals
            SetTile(pos, TileType.Dirt);
        }
    }

    public void RemoveUnit(Entity entity, int gridPos)
    {
        tileManager.GetTileDataAt(GetGridPos()).ClearOccupant();
    }
}
