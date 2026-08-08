using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
//This is the base class for making actions in the game that are availble after moving a unit
public class EntityAction : ScriptableObject
{
    public string actionName = "EntityAction";
    public string actionTrigger = "none";

    [Tooltip("width add additional width(0 is 1 tile, 1 is 3 tiles, ect..)")]
    public int width = 1;
    [Tooltip("length must be greater than 0")]
    public int length = 1;

    public AudioClip actionSound = null;

    //Returns the name of the action for scripts to identify actions with
    public virtual string GetName()
    {
        return actionName;
    }

    //Determines whether or not a unit can perform a given action
    public virtual bool IsPossible(Entity entity)
    {
        if (GetValidTargets(entity).Count <= 0 || !entity.IsActive())
        {
            return false;
        }
        return true;
    }
    //Holds the logic that is excuted when the action is chosen

    //finds any valid targets for the action
    public virtual List<Vector3Int> GetValidTargets(Entity caster, Entity entity)
    {
        //creates the empty list for return
        List<Vector3Int> targets = new List<Vector3Int>();

        //makes sure entity exists
        if (entity == null)
        {
            Debug.LogError("Invalid Entity");
            return targets;
        }

        //defines origin tile and tilemanager
        TileManager TM = FindFirstObjectByType<TileManager>();
        Vector3Int startPos = entity.GetGridPos();

        int range = 1;
        if(entity as Unit != null)
        {
            range = (entity as Unit).GetAttackRange();
        }

        Debug.Log("unit at " + startPos);

        //checks all four directions for targets
        for(int distx = -range; distx <= range; distx++)
        {
            for(int disty = -range; disty <= range; disty++)
            {
                //makes sure that the action is done withing the action range
                if(Mathf.Abs(distx) + Mathf.Abs(disty) > range)
                {
                    continue;
                }

                Vector3Int offset = new Vector3Int(distx, disty, 0);
                Vector3Int currentTile = startPos + offset;
                //checks valid targets in length
                for(int i = 0; i < length; i++)
                {
                    //checks valid targets width
                    for(int j = 0; j < width; j++)
                    {
                        //so first we find the tile i length away
                        Vector3Int checkTile = currentTile + new Vector3Int(offset.y * j, offset.x * j, 0);
                        TileData data = TM.GetTileDataAt(checkTile);
                        if(data == null)
                        {
                            continue;
                        }

                        //if there's no width we just check the center tile
                        if(j == 0)
                        {
                            //if the width is actionable
                            if(Action(caster, TM.GetTileDataAt(checkTile)))
                            {
                                targets.Add(currentTile);
                                continue;
                            }
                        }//if we have a width we go through all the widths
                        else
                        {
                            checkTile = currentTile + new Vector3Int(offset.y * j, offset.x * j, 0);
                            data = TM.GetTileDataAt(checkTile);
                            if(data != null)
                            {
                                if(Action(caster, TM.GetTileDataAt(checkTile)))
                                {
                                    targets.Add(currentTile);
                                    continue;
                                }
                            }
                            checkTile = currentTile + new Vector3Int(offset.y * j * -1, offset.x * j * -1, 0);
                            data = TM.GetTileDataAt(checkTile);
                            if(data != null)
                            {
                                if(Action(TM.GetTileDataAt(checkTile)))
                                {
                                    targets.Add(currentTile);
                                    continue;
                                }
                            }
                        }

                    }
                }
            }
        }
        return targets;
    }
    

    public virtual bool IsAOE()
    {
        if(width > 1 || length > 1)
        {
            return true;
        }
        return false;
    }

    //The unit here is the unit performing the action
    public virtual void PerformAt(Entity entity, List<Vector3Int> positions)
    {
        if(!IsAOE())
        {
            PerformAt(entity, positions[0]);
            return;
        }
        foreach(Vector3Int current in positions)
        {
            PerformAt(entity, current);
        }
    }

    //The unit here is the unit performing the action
    public virtual void PerformAt(Entity caster, Vector3Int pos)
    {
        TileManager TM = FindFirstObjectByType<TileManager>();
        Unit unit = caster as Unit;

        if(unit != null && unit.HasAnimator())
        {
            if(pos.x - unit.GetGridPos().x != 0)
            {
                unit.animator.SetFloat("facing", pos.x - unit.GetGridPos().x);
            }
            unit.SetAnimationTrigger(actionTrigger);
        }

        if(actionSound != null)
        {
            SoundManager.Instance.PlaySound(actionSound);
        }

        TileData data = TM.GetTileDataAt(pos);
        Vector3Int startPos = caster.GetGridPos();
        Vector3Int offset = pos - startPos;

        if(data != null)
        {
            for(int i = 1; i <= length; i++)
            {
                //checks valid targets width
                for(int j = 0; j < width; j++)
                {
                    //so first we find the tile i length away
                    Vector3Int currentTile = startPos + offset * i;
                    data = TM.GetTileDataAt(currentTile);
                    if(data == null)
                    {
                        continue;
                    }

                    //if there's no width we just check the center tile
                    if(j == 0)
                    {
                        //if the width is actionable
                        if(Action(caster, data))
                        {
                            PerformAt(caster, data);
                        }
                    }//if we have a width we go through all the widths
                    else
                    {
                        Vector3Int checkTile = currentTile + new Vector3Int(offset.y * j, offset.x * j, 0);
                        data = TM.GetTileDataAt(checkTile);
                        if(data != null)
                        {
                            if(Action(caster, TM.GetTileDataAt(checkTile)))
                            {
                                PerformAt(caster, data);
                            }
                        }
                        checkTile = currentTile + new Vector3Int(offset.y * j * -1, offset.x * j * -1, 0);
                        data = TM.GetTileDataAt(checkTile);
                        if(data != null)
                        {
                            if(Action(caster, TM.GetTileDataAt(checkTile)))
                            {
                                PerformAt(caster, data);
                            }
                        }
                    }
                }
            }
        }
    }

    //Used for AOE functions
    public virtual List<Vector3Int> GetExtensionTiles(Vector3Int target, Vector3Int casterPos)
    {
        if(!IsAOE())
        {
            return new List<Vector3Int>();
        }

        //TODO set up extension Tiles properly
        List<Vector3Int> tiles =  new List<Vector3Int>();
        TileManager TM = FindFirstObjectByType<TileManager>();
        //gets a directional offset between -1 and 1 to incriment the loops with
        
        Vector3Int offset = (target - casterPos);
        if(offset.x != 0)
        {
            offset.x = offset.x / Mathf.Abs(offset.x);
        }
        if(offset.y != 0)
        {
            offset.y = offset.y / Mathf.Abs(offset.y);
        }
        Vector3Int check = target - casterPos;

        for(int i = 0; i < length; i++)
        {
            for(int j = 0; j < width; j++)
            {
                check = target + offset * i;

                if(i == 0 && j == 0)
                {
                    continue;
                }
                if(j == 0)
                {
                    if(TM.GetTileDataAt(check) != null)
                    {
                        tiles.Add(check);
                    }
                }
                else
                {
                    Vector3Int side = new Vector3Int(offset.y * j, offset.x * j, 0);
                    check = target + offset * i + side;
                    if(TM.GetTileDataAt(check) != null)
                    {
                        tiles.Add(check);
                    }
                    check = target + offset * i - side;
                    if(TM.GetTileDataAt(check) != null)
                    {
                        tiles.Add(check);
                    }
                }
            }
        }

        return tiles;
    }

    //actually checks to see if the action can be done at position tilePos
    public virtual bool Action(Entity caster, TileData tileData){return false;}
    //actually preforms the Action on the tile
    public virtual void PerformAt(Entity caster, TileData tileData){}
    //Gets the direction from current caster to target (unit vector)
    protected Vector3Int GetDirection(Entity entity, Vector3Int targetTile)
    {
        Vector3Int delta = targetTile - entity.GetGridPos();
        int dx = delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1);
        int dy = delta.y == 0 ? 0 : (delta.y > 0 ? 1 : -1);
        return new Vector3Int(dx, dy, 0);
    }
}