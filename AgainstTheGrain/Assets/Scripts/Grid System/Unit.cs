using UnityEngine;
using System.Collections.Generic;

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
    [Header("General Unit Settings")]
    //Base Class for all character units
    public int id;

    //for determining if all actions have been completed
    public bool active = false;

    //sprite for the character
    public SpriteRenderer sprite;

    //Details about the unit
    public string unitName;
    public UnitType type;
    public bool isEnemy = false;
    public int moveAmt = 1;
    public string desc = "Put a description here!";

    //Needed
    [HideInInspector]
    public TileData tile;
    [HideInInspector]
    public Vector3Int pos;
    [SerializeField]
    protected int health = 1;
    [SerializeField]
    protected int damage = 1;

    //for buffs system
    protected List<Buff> currentBuffs = new List<Buff>();

    //for managing temporary state changes caused by other units

    protected TilemapManager manager;

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
        onGameStart();
    }

    public virtual void Refresh()
    {
        //progress the buffs after the unit is used for the turn
        CheckBuffs();
        //reset action points
        active = true;
        hideShade();
    }

    public void onGameStart()
    {
        Refresh();
    }

    public virtual void Deactivate()
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

    public virtual void moveToTile(Vector3Int position)
    {
        TileData newTile = manager.getTileData(position);

        if (newTile != null && newTile.GetOccupant() == null)
        {
            if (tile != null)
            {
                tile.RemoveOccupant();
            }
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

    public virtual void KillUnit()
    {
        //before destroying gameobject, remove unit from its matched tile
        pos = GetGridPosition();

        //remove self from owning tile
        if (tile != null)
        {
            tile.RemoveOccupant();
        }

        //dont count the difference check if the killed unit is an unactive animal
        int difference = 1;

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

    public void TakeDamage(int damage)
    {
        health = health - damage;
        if (health <= 0)
        {
            KillUnit();
        }
    }

    public int GetHealth()
    {
        return health;
    }

    public bool isUseableUnit()
    {
        return true;
    }

    public int getEffectiveMoveAmt()
    {
        int moveBuff = 0;
        foreach (Buff buff in currentBuffs)
        {
            if (buff.type == BuffType.Movement)
            {
                moveBuff += buff.strength;
            }
        }

        return moveBuff + moveAmt;
    }

    public int getEffectiveDamage()
    {
        int damageBuff = 0;
        foreach (Buff buff in currentBuffs)
        {
            if (buff.type == BuffType.Damage)
            {
                damageBuff += buff.strength;
            }
        }

        return damageBuff + damage;
    }

    //add a buff to the set of current buffs
    public void buffUnit(Buff buff)
    {
        //add the buff to the current list of active buffs
        currentBuffs.Add(buff);
        //do something to add an indicator
        Debug.Log("Buff added to " + unitName + " with name: " + buff.name + " and strength " + buff.strength);
    }

    public void CheckBuffs()
    {
        if (currentBuffs.Count <= 0)
        {
            return;
        }

        //use reverse for loop to traverse backwards for removals
        for (int i = currentBuffs.Count-1; i >= 0; i--)
        {
            //update the timers on the buffs in the active buffs
            currentBuffs[i].increment();
            if (currentBuffs[i].isExpired())
            {
                currentBuffs.Remove(currentBuffs[i]);
            }
        }
    }

    public void ClearBuffByName(string buffName)
    {
        if (currentBuffs.Count <= 0)
        {
            return;
        }

        foreach (Buff buff in currentBuffs)
        {
            if (buff.name == buffName)
            {
                currentBuffs.Remove(buff);
            }
        }
    }

}
