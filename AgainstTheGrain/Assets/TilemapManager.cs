
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.RuleTile.TilingRuleOutput;

public enum TileType
{
    None,
    Grass,
    Dirt,
    Soil
}


public class TilemapManager : MonoBehaviour
{
    public static TilemapManager instance;

    //References to placeholder and display tilemaps for generating visuals
    public Tilemap placeholderMap;
    public Tilemap displayMap;
    public Tilemap infoMap;
    public Tilemap detailsMap;

    //Used for calculating positions of nearby tiles for generating the tilemap objects
    private static Vector3Int[] NEIGHBORS = new Vector3Int[]
    {
        //Offsets for referencing specific tiles
        new Vector3Int(0,0,0), //Top Right
        new Vector3Int(1,0,0), //Top Left
        new Vector3Int(0,1,0), //Bottom Right
        new Vector3Int(1,1,0) //Bottom Left
    };

    private static Vector3Int[] DIRECTIONS = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0)
    };

    //Dictionary for defining the different tile combinations
    //Depending on the nearby tiles, determine the correct combination to show
    private Dictionary< Tuple<TileType, TileType, TileType, TileType>, Tile> convertToTile;
    //Dicionary for getting quick references to the data at a given tile (occupants, terrain etc)
    private Dictionary<Vector3Int, TileData> tilePosToData;

    //Placeholders for tile calculation checks
    public TileBase grassTile;
    public TileBase dirtTile;
    public TileBase soilTile;

    //for showing movement possiblities
    public TileBase available;
    public TileBase enemyAvailable;

    public TileBase impassable;

    public TileBase greenAvailible;

    public TileBase reachTile;
    public TileBase greyReachTile;

    //Used to store the visual output for the tilemap, referennced by the dictionary for correct tiles
    public Tile[] tiles;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InitManager();
    }

    private void Start()
    {
        //initialize the 
        
        createDisplayMap();
    }

    public void InitManager()
    {
        tilePosToData = new() { };
        convertToTile = new()
        {
            //Key indexes referenced from this repo: https://github.com/jess-hammer/dual-grid-tilemap-system-unity/blob/main/Assets/Scripts/DualGridTilemap.cs
            {new (TileType.Grass, TileType.Grass, TileType.Grass, TileType.Grass), tiles[6] }, //Full Grass
            {new (TileType.Dirt, TileType.Dirt, TileType.Dirt, TileType.Dirt), tiles[12] }, //Full Dirt
            {new (TileType.Soil, TileType.Soil, TileType.Soil, TileType.Soil), tiles[22] }, //full soil

            //Grass next to dirt
            {new (TileType.Dirt, TileType.Dirt, TileType.Dirt, TileType.Grass), tiles[13]}, // OUTER_BOTTOM_RIGHT
            {new (TileType.Dirt, TileType.Dirt, TileType.Grass, TileType.Dirt), tiles[0]}, // OUTER_BOTTOM_LEFT
            {new (TileType.Dirt, TileType.Grass, TileType.Dirt, TileType.Dirt), tiles[8]}, // OUTER_TOP_RIGHT
            {new (TileType.Grass, TileType.Dirt, TileType.Dirt, TileType.Dirt), tiles[15]}, // OUTER_TOP_LEFT
            {new (TileType.Dirt, TileType.Grass, TileType.Dirt, TileType.Grass), tiles[1]}, // EDGE_RIGHT
            {new (TileType.Grass, TileType.Dirt,TileType.Grass, TileType.Dirt), tiles[11]}, // EDGE_LEFT
            {new (TileType.Dirt, TileType.Dirt,TileType.Grass,TileType.Grass), tiles[3]}, // EDGE_BOTTOM
            {new (TileType.Grass,TileType.Grass, TileType.Dirt, TileType.Dirt), tiles[9]}, // EDGE_TOP
            {new (TileType.Dirt,TileType.Grass,TileType.Grass,TileType.Grass), tiles[5]}, // INNER_BOTTOM_RIGHT
            {new (TileType.Grass, TileType.Dirt,TileType.Grass,TileType.Grass), tiles[2]}, // INNER_BOTTOM_LEFT
            {new (TileType.Grass,TileType.Grass, TileType.Dirt,TileType.Grass), tiles[10]}, // INNER_TOP_RIGHT
            {new (TileType.Grass,TileType.Grass,TileType.Grass, TileType.Dirt), tiles[7]}, // INNER_TOP_LEFT
            {new (TileType.Dirt,TileType.Grass,TileType.Grass, TileType.Dirt), tiles[14]}, // DUAL_UP_RIGHT
            {new (TileType.Grass, TileType.Dirt, TileType.Dirt,TileType.Grass), tiles[4]}, // DUAL_DOWN_RIGHT

            //Soil next to dirt
            {new (TileType.Dirt, TileType.Dirt, TileType.Dirt, TileType.Soil), tiles[29]}, // OUTER_BOTTOM_RIGHT
            {new (TileType.Dirt, TileType.Dirt, TileType.Soil, TileType.Dirt), tiles[16]}, // OUTER_BOTTOM_LEFT
            {new (TileType.Dirt, TileType.Soil, TileType.Dirt, TileType.Dirt), tiles[24]}, // OUTER_TOP_RIGHT
            {new (TileType.Soil, TileType.Dirt, TileType.Dirt, TileType.Dirt), tiles[31]}, // OUTER_TOP_LEFT
            {new (TileType.Dirt, TileType.Soil, TileType.Dirt, TileType.Soil), tiles[17]}, // EDGE_RIGHT
            {new (TileType.Soil, TileType.Dirt,TileType.Soil, TileType.Dirt), tiles[27]}, // EDGE_LEFT
            {new (TileType.Dirt, TileType.Dirt,TileType.Soil,TileType.Soil), tiles[19]}, // EDGE_BOTTOM
            {new (TileType.Soil,TileType.Soil, TileType.Dirt, TileType.Dirt), tiles[25]}, // EDGE_TOP
            {new (TileType.Dirt,TileType.Soil,TileType.Soil,TileType.Soil), tiles[21]}, // INNER_BOTTOM_RIGHT
            {new (TileType.Soil, TileType.Dirt,TileType.Soil,TileType.Soil), tiles[18]}, // INNER_BOTTOM_LEFT
            {new (TileType.Soil,TileType.Soil, TileType.Dirt,TileType.Soil), tiles[26]}, // INNER_TOP_RIGHT
            {new (TileType.Soil,TileType.Soil,TileType.Soil, TileType.Dirt), tiles[23]}, // INNER_TOP_LEFT
            {new (TileType.Dirt,TileType.Soil,TileType.Soil, TileType.Dirt), tiles[30]}, // DUAL_UP_RIGHT
            {new (TileType.Soil, TileType.Dirt, TileType.Dirt,TileType.Soil), tiles[20]}, // DUAL_DOWN_RIGHT

            //Grass next to soil
            {new (TileType.Soil, TileType.Soil, TileType.Soil, TileType.Grass), tiles[13+32]}, // OUTER_BOTTOM_RIGHT
            {new (TileType.Soil, TileType.Soil, TileType.Grass, TileType.Soil), tiles[0+32]}, // OUTER_BOTTOM_LEFT
            {new (TileType.Soil, TileType.Grass, TileType.Soil, TileType.Soil), tiles[8+32]}, // OUTER_TOP_RIGHT
            {new (TileType.Grass, TileType.Soil, TileType.Soil, TileType.Soil), tiles[15+32]}, // OUTER_TOP_LEFT
            {new (TileType.Soil, TileType.Grass, TileType.Soil, TileType.Grass), tiles[1+32]}, // EDGE_RIGHT
            {new (TileType.Grass, TileType.Soil,TileType.Grass, TileType.Soil), tiles[11+32]}, // EDGE_LEFT
            {new (TileType.Soil, TileType.Soil,TileType.Grass,TileType.Grass), tiles[3+32]}, // EDGE_BOTTOM
            {new (TileType.Grass,TileType.Grass, TileType.Soil, TileType.Soil), tiles[9+32]}, // EDGE_TOP
            {new (TileType.Soil,TileType.Grass,TileType.Grass,TileType.Grass), tiles[5+32]}, // INNER_BOTTOM_RIGHT
            {new (TileType.Grass, TileType.Soil,TileType.Grass,TileType.Grass), tiles[2+32]}, // INNER_BOTTOM_LEFT
            {new (TileType.Grass,TileType.Grass, TileType.Soil,TileType.Grass), tiles[10+32]}, // INNER_TOP_RIGHT
            {new (TileType.Grass,TileType.Grass,TileType.Grass, TileType.Soil), tiles[7+32]}, // INNER_TOP_LEFT
            {new (TileType.Soil,TileType.Grass,TileType.Grass, TileType.Soil), tiles[14+32]}, // DUAL_UP_RIGHT
            {new (TileType.Grass, TileType.Soil, TileType.Soil,TileType.Grass), tiles[4+32]}, // DUAL_DOWN_RIGHT

            //Grass Soil Dirt Combos

            {new (TileType.Dirt, TileType.Grass, TileType.Soil, TileType.Grass), tiles[48] },
            {new (TileType.Soil, TileType.Dirt, TileType.Grass, TileType.Grass), tiles[49] },
            {new (TileType.Soil, TileType.Grass, TileType.Dirt, TileType.Grass), tiles[50] },
            {new (TileType.Dirt, TileType.Soil, TileType.Grass, TileType.Grass), tiles[51] },

            {new (TileType.Dirt, TileType.Grass, TileType.Grass, TileType.Soil), tiles[52] },
            {new (TileType.Grass, TileType.Dirt, TileType.Soil, TileType.Grass), tiles[53] },

            {new (TileType.Grass, TileType.Grass, TileType.Dirt, TileType.Soil), tiles[54] },
            {new (TileType.Grass, TileType.Soil, TileType.Grass, TileType.Dirt), tiles[55] },
            {new (TileType.Grass, TileType.Grass, TileType.Soil, TileType.Dirt), tiles[56] },
            {new (TileType.Grass, TileType.Dirt, TileType.Grass, TileType.Soil), tiles[57] },

            {new (TileType.Grass, TileType.Soil, TileType.Dirt, TileType.Grass), tiles[58] },
            {new (TileType.Soil, TileType.Grass, TileType.Grass, TileType.Dirt), tiles[59] },

            {new (TileType.Dirt, TileType.Soil, TileType.Soil, TileType.Grass), tiles[60] },
            {new (TileType.Soil, TileType.Dirt, TileType.Grass, TileType.Soil), tiles[61] },
            {new (TileType.Soil, TileType.Grass, TileType.Dirt, TileType.Soil), tiles[62] },
            {new (TileType.Grass, TileType.Soil, TileType.Soil, TileType.Dirt), tiles[63] },

            //Hard edges

            {new (TileType.Soil, TileType.Dirt, TileType.Dirt, TileType.Grass), tiles[64] },
            {new (TileType.Dirt, TileType.Soil, TileType.Grass, TileType.Dirt), tiles[65] },
            {new (TileType.Dirt, TileType.Grass, TileType.Soil, TileType.Dirt), tiles[66] },
            {new (TileType.Grass, TileType.Dirt, TileType.Dirt, TileType.Soil), tiles[67] },
            {new (TileType.Dirt, TileType.Soil, TileType.Grass, TileType.Soil), tiles[68] },
            {new (TileType.Grass, TileType.Dirt, TileType.Soil, TileType.Soil), tiles[69] },
            {new (TileType.Soil, TileType.Dirt, TileType.Grass, TileType.Dirt), tiles[70] },
            {new (TileType.Grass, TileType.Soil, TileType.Dirt, TileType.Dirt), tiles[71] },
            {new (TileType.Soil, TileType.Soil, TileType.Dirt, TileType.Grass), tiles[72] },
            {new (TileType.Soil, TileType.Grass, TileType.Soil, TileType.Dirt), tiles[73] },
            {new (TileType.Dirt, TileType.Dirt, TileType.Soil, TileType.Grass), tiles[74] },
            {new (TileType.Dirt, TileType.Grass, TileType.Dirt, TileType.Soil), tiles[75] },

            //Hard edges Flipped

            {new (TileType.Soil, TileType.Grass, TileType.Dirt, TileType.Dirt), tiles[76] },
            {new (TileType.Dirt, TileType.Soil, TileType.Dirt, TileType.Grass), tiles[77] },
            {new (TileType.Dirt, TileType.Grass, TileType.Soil, TileType.Soil), tiles[78] },
            {new (TileType.Soil, TileType.Dirt, TileType.Soil, TileType.Grass), tiles[79] },
            {new (TileType.Grass, TileType.Dirt, TileType.Soil, TileType.Dirt), tiles[80] },
            {new (TileType.Dirt, TileType.Dirt, TileType.Grass, TileType.Soil), tiles[81] },
            {new (TileType.Grass, TileType.Soil, TileType.Dirt, TileType.Soil), tiles[82] },
            {new (TileType.Soil, TileType.Soil, TileType.Grass, TileType.Dirt), tiles[83] },

        };
    }

    //Used to check what kind of tile is being stored in the placeholder map
    public TileType getPlaceHolderAt(Vector3Int coords)
    {
        if (placeholderMap.GetTile(coords) == grassTile)
        {
            return TileType.Grass;
        }
        else if (placeholderMap.GetTile(coords) == dirtTile)
        {
            return TileType.Dirt;
        }
        else if (placeholderMap.GetTile(coords) == soilTile)
        {
            return TileType.Soil;
        }
        return TileType.None;
    }

    private TileType typeFromTile(TileBase tile)
    {
        if (tile == grassTile)
        {
            return TileType.Grass;
        }
        else if (tile == dirtTile)
        {
            return TileType.Dirt;
        }
        else if (tile == soilTile)
        {
            return TileType.Soil;
        }
        return TileType.None;
    }

    public TileData getTileData(Vector3Int coords)
    {
        tilePosToData.TryGetValue(coords, out TileData temp);
        TileData tileData = temp;
        if (tileData != null)
        {
            return tileData;
        }
        //otherwise pass nothing back
        return null;
    }

    public void SetTileByType(TileType type, Vector3Int pos)
    {
        if (type == TileType.Grass)
        {
            SetTile(pos, grassTile);
        }
        else if (type == TileType.Soil)
        {
            SetTile(pos, soilTile);
        }
        else if (type == TileType.Dirt)
        {
            SetTile(pos, dirtTile);
        }
    }

    public void SetTile(Vector3Int coords, TileBase tile)
    {
        //Set the actual tile
        placeholderMap.SetTile(coords, tile);
        //update the visual tile based on the 4 neighboring tiles
        setDisplayTile(coords);

        //Update the information in the tile data
        if (tilePosToData.ContainsKey(coords)){
            //Update existing tile type based on new type
            if (tilePosToData[coords] != null)
            {

                tilePosToData[coords].type = typeFromTile(tile);
                if (typeFromTile(tile) == TileType.None)
                {
                    tilePosToData[coords].canWalk = false;
                }
            }
                
        }
        else
        {
            //create a new tiledata object and store it in the dictionary
            //get a variable type to store the correct type
            if ((typeFromTile(tile) != TileType.None)){

                tilePosToData[coords] = new TileData(typeFromTile(tile), 1, true);
            }else
            {
                tilePosToData[coords] = new TileData(typeFromTile(tile), 1, false);
            }
        }
    }

    public void ToggleUnwalkable(Vector3Int coords)
    {
        //Update the information in the tile data
        if (tilePosToData.ContainsKey(coords))
        {
            //Update existing tile type based on new type
            if (tilePosToData[coords] != null)
            {
                //flip the current status of whatever the tile is
                tilePosToData[coords].canWalk = !tilePosToData[coords].canWalk;
                if (tilePosToData[coords].canWalk)
                {
                    Debug.Log("This tile is now walkable");
                }
                else
                {
                    Debug.Log("This tile is now UNwalkable");
                }
            }

        }
    }


    private Tile calculateDisplayTile(Vector3Int coords)
    {
        TileType tR = getPlaceHolderAt(coords - NEIGHBORS[0]);
        TileType tL = getPlaceHolderAt(coords - NEIGHBORS[1]);
        TileType bR = getPlaceHolderAt(coords - NEIGHBORS[2]);
        TileType bL = getPlaceHolderAt(coords - NEIGHBORS[3]);

        //Assemble the key for the dicitionary
        Tuple<TileType, TileType, TileType, TileType> keyTuple = new(tL, tR, bL, bR);

        if (convertToTile.TryGetValue(keyTuple, out Tile tempTile))
        {
            //Return the Tile matching the neighbors if found
            return tempTile;
        }

        //If no tile is found, do not display anythin
        return null;
    }

    private void setDisplayTile(Vector3Int coords)
    {
        //Update the four surrounding tiles (based on neighbors needed for calculations
        for (int i = 0; i < 4; i++)
        {
            
            Vector3Int tempPosition = coords + NEIGHBORS[i];
            displayMap.SetTile(tempPosition, calculateDisplayTile(tempPosition));
        }
    }

    //Generates the visual output of the tilemap by analyzing each tile
    public void createDisplayMap()
    {
        for (int i = -16; i < 16; i++)
        {
            for (int j = -16; j < 16; j++)
            {
                Vector3Int pos = new Vector3Int(i, j, 0);
                if (placeholderMap.HasTile(pos))
                {
                    TileBase tile = placeholderMap.GetTile(pos);
                    if (tile == grassTile)
                    {
                        SetTile(pos, grassTile);
                    }
                    else if (tile == soilTile)
                    {
                        SetTile(pos, soilTile);
                    }
                    else if (tile == dirtTile)
                    {
                        SetTile(pos, dirtTile);
                    }
                }
                else
                {
                    SetTile(pos, grassTile);
                }

                //if there is an obstruction, dont allow walking
                if (detailsMap.HasTile(pos))
                {
                    getTileData(pos).canWalk = false;
                }

                //Generate Visual Output Tile
                setDisplayTile(pos);
            }
        }
    }

    //highlight all tiles that a unit can move to
    //https://www.geeksforgeeks.org/breadth-first-search-or-bfs-for-a-graph/#
    public List<Tuple<Vector3Int, bool>> ShowMoveableTiles(Vector3Int startPos, int moveDistance)
    {
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        List<Tuple<Vector3Int, bool>> valid = new List<Tuple<Vector3Int, bool>>();
        valid.Add(new Tuple<Vector3Int, bool>(startPos, true));
        queue.Enqueue(startPos);
        visited.Add(startPos);
        costSoFar[startPos] = 0;

        //determine what tiles to show whether enemy or non enemy
        TileData start = getTileData(startPos);
        bool enemy = start.occupant.isEnemy;

        //perform a breathe first search
        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            int currentCost = costSoFar[current];

            //check for new tiles in every direction
            foreach (var direction in DIRECTIONS)
            {
                //get the neighboring position on the grid
                Vector3Int neighbor = current + direction;

                //calculate space based moves rather than discance
                bool diagonal = Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 2;
                //determine if needs two moves to reach
                int moveCost = diagonal ? 2 : 1;
                int newCost = currentCost + moveCost;

                if (newCost > moveDistance + 1)
                    continue;

                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;

                    if (!tilePosToData.ContainsKey(neighbor)) continue;

                    var tileData = tilePosToData[neighbor];

                    if (newCost <= moveDistance)
                    {
                        if (tileData.canWalk && tileData.occupant == null)
                        {
                            queue.Enqueue(neighbor);
                            if (!enemy)
                            {
                                infoMap.SetTile(neighbor, available);
                            }
                            else
                            {
                                infoMap.SetTile(neighbor, enemyAvailable);
                            }
                            valid.Add(new Tuple<Vector3Int, bool>(neighbor, true));
                        }
                        else if (tileData.crop != null)
                        {
                            infoMap.SetTile(neighbor, greenAvailible);
                            valid.Add(new Tuple<Vector3Int, bool>(neighbor, false));
                        }
                        else if (neighbor != startPos)
                        {
                            infoMap.SetTile(neighbor, impassable);
                            valid.Add(new Tuple<Vector3Int, bool>(neighbor, false));
                        }
                    }
                    else if (newCost == moveDistance + 1)
                    {
                        // One tile beyond movement range, indicate interactable but not movable
                        if (!enemy)
                        {

                            infoMap.SetTile(neighbor, reachTile);
                        }
                        else
                        {
                            infoMap.SetTile(neighbor, greyReachTile);
                        }
                        valid.Add(new Tuple<Vector3Int, bool>(neighbor, false));
                    }
                }
            }
        }

        return valid;

    }

    //copy code for bfs function above, but cut the fat for tile vision and reach tiles
    //also references: https://www.geeksforgeeks.org/breadth-first-search-or-bfs-for-a-graph/#
    public List<Vector3Int> GetMoveableTiles(Vector3Int startPos, int moveDistance)
    {
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        List<Vector3Int> valid = new List<Vector3Int>();

        queue.Enqueue(startPos);
        visited.Add(startPos);
        valid.Add(startPos);
        costSoFar[startPos] = 0;

        //determine what tiles to show whether enemy or non enemy
        TileData start = getTileData(startPos);

        //perform a breathe first search
        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            int currentCost = costSoFar[current];

            //check for new tiles in every direction
            foreach (var direction in DIRECTIONS)
            {
                //get the neighboring position on the grid
                Vector3Int neighbor = current + direction;

                //calculate space based moves rather than discance
                bool diagonal = Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 2;
                //determine if needs two moves to reach
                int moveCost = diagonal ? 2 : 1;
                int newCost = currentCost + moveCost;

                if (newCost > moveDistance + 1)
                    continue;

                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;

                    if (!tilePosToData.ContainsKey(neighbor)) continue;

                    var tileData = tilePosToData[neighbor];

                    if (newCost <= moveDistance)
                    {
                        //only consider it valid if theres nothing blocking the way
                        if (tileData.canWalk && tileData.occupant == null)
                        {
                            queue.Enqueue(neighbor);
                            valid.Add(neighbor);
                        }
                    }
                }
            }
        }

        return valid;
    }

    public Vector3Int NearestPlayerUnit(Vector3Int start)
    {
        //Get reference to the container that stores the player units
        GameObject container = GameManager.instance.playerUnits;
        Vector3Int nearestTile = start;

        int closestYet = int.MaxValue;
        foreach (UnityEngine.Transform unit in container.transform)
        {
            Vector3Int pos = unit.GetComponent<Unit>().GetGridPosition();
            int dist = TileDistance(start, pos);
            //Debug.Log("Unit At: " + "[" + pos.x + "," +pos.y + "] is " + dist + " away!" );
            
            //if current unit is now the closest we've seen, keep track of its position
            if (dist < closestYet)
            {
                closestYet = dist;
                nearestTile = pos;
            }
        }

        //Debug.Log("The closest unit is at position [" + nearestTile.x + "," + nearestTile.y + "] " + closestYet + " moves away!");
        return nearestTile;
    }

    //Needed for finding total distance based on grid movements needed
    public int TileDistance(Vector3Int start, Vector3Int finish)
    {
        int xMoves = Mathf.Abs(start.x - finish.x);
        int yMoves = Mathf.Abs(start.y - finish.y);

        return xMoves + yMoves;
    }

    //used for updating infomap
    public void ShowAvailable(Vector3Int position)
    {
        infoMap.SetTile(position, available);
    }

    public void HideInfo(Vector3Int position)
    {
        infoMap.SetTile(position, null);
    }

    public void ShowReach(Vector3Int position)
    {
        infoMap.SetTile(position, reachTile);
    }

    public List<Tuple<Vector3Int, bool>> GetEnemiesNearby(Vector3Int position, bool showTile)
    {
        List<Tuple<Vector3Int, bool>> valid = new List<Tuple<Vector3Int, bool>>();

        for (int i=0; i<4; i++)
        {
            Vector3Int location = position + DIRECTIONS[i];
            //try to find unit at neigboring location
            Unit temp = getTileData(location).occupant;
            if ( temp != null && temp.isEnemy)
            {
                valid.Add(new Tuple<Vector3Int, bool>(location, true));
                if (showTile)
                {

                    infoMap.SetTile(location, impassable);
                }
            }
        }
        return valid;
    }

    public List<Tuple<Vector3Int, bool>> GetCropsNearby(Vector3Int position, bool showTile)
    {
        List<Tuple<Vector3Int, bool>> valid = new List<Tuple<Vector3Int, bool>>();

        for (int i = 0; i < 4; i++)
        {
            Vector3Int location = position + DIRECTIONS[i];
            //try to find unit at neigboring location
            CropObject temp = getTileData(location).crop;
            if (temp != null)
            {
                valid.Add(new Tuple<Vector3Int, bool>(location, true));
                if (showTile)
                {

                    infoMap.SetTile(location, available);
                }
            }
        }
        return valid;
    }

    public List<Tuple<Vector3Int, bool>> GetHarvestableCropsNearby(Vector3Int position, bool showTile)
    {
        List<Tuple<Vector3Int, bool>> valid = new List<Tuple<Vector3Int, bool>>();

        for (int i = 0; i < 4; i++)
        {
            Vector3Int location = position + DIRECTIONS[i];
            //try to find unit at neigboring location
            CropObject temp = getTileData(location).crop;
            if (temp != null && temp.harvestable)
            {
                valid.Add(new Tuple<Vector3Int, bool>(location, true));
                if (showTile)
                {

                    infoMap.SetTile(location, available);
                }
            }
        }
        return valid;
    }

    public List<Tuple<Vector3Int, bool>> GetDirtNearby(Vector3Int position, bool showTile)
    {
        List<Tuple<Vector3Int, bool>> valid = new List<Tuple<Vector3Int, bool>>();

        for (int i = 0; i < 4; i++)
        {
            Vector3Int location = position + DIRECTIONS[i];
            //try to find unit at neigboring location
            TileData tile = getTileData(location);
            if (getPlaceHolderAt(location) == TileType.Dirt && tile.occupant == null && tile.crop == null )
            {
                valid.Add(new Tuple<Vector3Int, bool>(location, true));
                if (showTile)
                {
                    infoMap.SetTile(location, available);
                }
            }
        }
        return valid;
    }
}
