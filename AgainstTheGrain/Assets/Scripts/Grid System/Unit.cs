using UnityEngine;
using System.Collections.Generic;
using System.Collections;

//forces a sprite renderer

public enum UnitType{
    Farmer,
    Infantry,
    Hybrid,
    Animal
}
[RequireComponent(typeof(SpriteRenderer))]
public class Unit : MonoBehaviour
{
    //Base Class for all character units
    public int id;

    //for interactions with the world
    public int maxAPs = 2;
    public int APs = 0;

    //for determining if all actions have been completed
    public bool active = false;

    //sprite for the character
    public SpriteRenderer sprite;

    //Details about the unit
    public string unitName;
    public UnitType type;
    public bool isEnemy;
    public int moveAmt;

    public string desc;
    public TileData tile;
    public Vector3Int pos;

    public int maxHealth;
    private int health;
    public int damage;

    public bool isFed = false;
    public int feedNeed = 1;

    private TilemapManager manager;

    //Display information about the unit
    public void PrintInfo()
    {
        Debug.Log("Unit ID: " + id + " | Unit Name: " + unitName + " | Position: [" + pos.x + "," + pos.y + "]");
        Debug.Log(desc);
    }

    private void Awake()
    {
        manager = TilemapManager.instance;
        sprite = GetComponent<SpriteRenderer>();
        
    }

    public void Start()
    {
        pos = GetGridPosition();
        health = maxHealth;
        Refresh();
    }

    public void Refresh()
    {
        //reset action points
        APs = maxAPs;
        active = true;
        hideShade();
    }

    public void Deactivate()
    {
        showShade();
        active = false;
    }

    public void showShade()
    {
        sprite.color = new Color(0.5f, 0.5f, 0.5f);
    }

    public void hideShade()
    {
        sprite.color = Color.white;
    }

    public void moveToTile(Vector3Int position)
    {
        TileData newTile = manager.getTileData(position);

        if (newTile != null && newTile.GetOccupant() == null)
        {
            tile.RemoveOccupant();
            newTile.SetOccupant(this);
            tile = newTile;
            pos = position;
            gameObject.transform.position = position + new Vector3(0.5f,0.5f,0);
        }
    }
    
    //Needed for the enemy script for pathfinding
    public Vector3Int GetGridPosition()
    {
        //get the current unit location
        Vector3 location = this.transform.position;

        int xCoord = Mathf.FloorToInt(location.x - 0.5f);
        int yCoord = Mathf.FloorToInt(location.y - 0.5f);
        Vector3Int returnVal = new Vector3Int(xCoord, yCoord, 0);
        return returnVal;
    }

    public Vector3Int MoveTowardsNearestUnit(bool isPlayerUnit)
    {
        if (!active)
        {
            return GetGridPosition();
        }

        TilemapManager manager = TilemapManager.instance;
        //call several functions from the tilemap manager to determine where to move
        //find the spot we are trying to move towards
        pos = GetGridPosition();
        Vector3Int idealTile = pos;
        Vector3Int moveLocation = pos;

        if (isPlayerUnit)
        {
            idealTile = manager.NearestPlayerUnit(pos);
        }
        else
        {
            //get a function for nearest enemy
        }

        //if there is nothing to move towards, return
        if (idealTile == pos)
        {
            return GetGridPosition();
        }

        //get the all the valid tile locations for the unit
        List<Vector3Int> validTiles = manager.GetMoveableTiles(pos, moveAmt);


        //use similar method of finding ideal location
        int closestYet = int.MaxValue;

        //find the nearest valid tile to the location we found earlier
        foreach (Vector3Int location in validTiles)
        {
            //check if the distance is closer than the closest found thus far
            //distance between current valid tile, and the ideal unit position
            int dist = manager.TileDistance(idealTile, location);
            if (dist < closestYet)
            {
                closestYet = dist;
                moveLocation = location;
                //if found a spot adjacent to player unit, a closest tile has been found
                if (dist == 1)
                {
                    break;
                }

            }
        }

        //determine whether or not to move if already 1 away
        if (closestYet != 1 && manager.TileDistance(idealTile, pos) == 1)
        {
            //if not necessary to move, dont do it, return
            return GetGridPosition();
        }

        return moveLocation;
    }

    //use for visual output for enemies
    //highlights unit location and target location
    public IEnumerator AnimatedMove()
    {
        //highlight tile

        

        Vector3Int moveTo = MoveTowardsNearestUnit(true);
        manager = TilemapManager.instance;
        manager.ShowReach(pos);
        manager.ShowAvailable(moveTo);

        yield return new WaitForSeconds(1f);

        manager.HideInfo(pos);
        manager.HideInfo(moveTo);
        //perform the movement operation
        moveToTile(moveTo);
    }

    public IEnumerator ProcessTurn()
    {
        if (!GameManager.instance.gameOver)
        {

            yield return StartCoroutine(AnimatedMove());

            Vector3Int idealTile = manager.NearestPlayerUnit(pos);
        
            //if there is a nearby unit, attack it
            if (manager.TileDistance(pos, idealTile) == 1)
            {
                //check for a unit at the ideal tile
                TileData opponentTile = manager.getTileData(idealTile);
                if (opponentTile.occupant != null)
                {
                    opponentTile.occupant.TakeDamage(damage);
                }
            }

            Deactivate();

            //check if the robots have won
            yield return GameManager.instance.CheckEndState();
        }
    }

    public void TakeDamage(int damage)
    {
        health = health - damage;
        if (health <= 0)
        {
            KillUnit();
        }
    }

    public void KillUnit()
    {
        //before destroying gameobject, remove unit from its matched tile
        pos = GetGridPosition();

        //remove self from owning tile
        tile.RemoveOccupant();

        //dont count the difference check if the killed unit is an unactive animal
        int difference = (type != UnitType.Animal || isFed) ? 1 : 0;

        //check if end game state is reached based on this unit dying
        GameManager GM = GameManager.instance;
        if (isEnemy && GM.GetNumEnemies() <= 1)
        {
            GM.ShowVictoryScreen();
            Destroy(this.gameObject);
        }
        else if (!isEnemy && GM.GetNumActivePlayers() - difference == 0)
        {
            
            GM.ShowDefeatScreen();
            Destroy(this.gameObject);
        }
        Destroy(this.gameObject);
    }

    public int GetHealth()
    {
        return health;
    }
}
