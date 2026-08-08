using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    [Header("General Settings")]
    public TileManager tileManager;
    public TileHelper tileHelper;
    public GameManager gameManager;

    public void Awake()
    {
        tileManager = FindFirstObjectByType<TileManager>();
        tileHelper = FindFirstObjectByType<TileHelper>();
        gameManager = FindFirstObjectByType<GameManager>();
        
    }

    public Vector3Int FindTarget(Unit unit)
    {
        Debug.Log("Test 2");
        Vector3Int target = new Vector3Int(0,0,-1);
        //choose target based on iq
        if (unit.iq < 2)
        {
            target = FindEasy(unit);
        }
        else if (unit.iq < 5)
        {
            target = FindMedium(unit);
        }
        else
        {
            target = FindHard(unit);
        }

        // Still nothing, clear target
        if (target.z == -1)
        {
            Debug.Log("No Pathable Primary/Any Target found");
        }
        Debug.Log("Found Target: " + target.x + " " + target.y + " " + target.z);
        return target;
    }

    /*
        targeting for "Easy" enemies
        
        targets clostest primary target

        if there is no targetable primary target it chooses the closest unit
    */
    Vector3Int FindEasy(Unit unit)
    {
        //gets all primary targets
        List<Vector3Int> targets = FindPositions(true, unit);

        Vector3Int best = new Vector3Int(0, 0, -1);
        int bestTTR = int.MaxValue;

        foreach (Vector3Int t in targets)
        {
            //get the path for the next potential target
            List<Vector3Int> path = tileHelper.TilePath(unit.GetGridPos(), t, unit);

            // Skip paths that return no path (only 1 item in list)
            if (path.Count <= 1)
                continue;

            //uses turnstoreach to find the "best" target
            int ttr = TurnsToReach(path, unit);
            if (ttr < bestTTR)
            {
                bestTTR = ttr;
                best = t;
            }
        }

        // if an accesible primary target was found return
        if(best.z != -1)
        {
            return best;
        }

        //now we look for any target
        targets = FindPositions(false, unit);

        foreach (Vector3Int t in targets)
        {
            //get the path for the next potential target
            List<Vector3Int> path = tileHelper.TilePath(unit.GetGridPos(), t, unit);

            // Skip paths that return no path (only 1 item in list)
            if (path.Count == 1)
                continue;

            //uses turnstoreach to find the "best" target
            int ttr = TurnsToReach(path, unit);
            if (ttr < bestTTR)
            {
                bestTTR = ttr;
                best = t;
            }
        }

        return best;
    }

    /*
        targeting for "Medium" enemies

        targets closest primary target
        or
        secondary target that takes -1 turn to kill
    */
    Vector3Int FindMedium(Unit unit)
    {
        //get list of all targets
        List<Vector3Int> targets = FindPositions(false, unit);

        Vector3Int best = new Vector3Int(0, 0, -1);
        int bestTTK = int.MaxValue;

        foreach (Vector3Int t in targets)
        {
            //get the path for the next potential target
            List<Vector3Int> path = tileHelper.TilePath(unit.GetGridPos(), t, unit);

            // Skip paths that return no path (only 1 item in list)
            if (path.Count <= 1)
                continue;

            //uses turnstokill to find the "best" target
            int ttk = TurnsToKill(path, unit, t);

            //Gives us information about the current entity to calculate bonuses to influence targeting
            Entity potentialTarget = tileManager.GetTileDataAt(t).GetOccupyingEntity();
            if (potentialTarget == null)
                continue;

            //Only consider fences if they block the path to something
            if (potentialTarget is Structure s)
            {
                if (!IsStructureBlockingPath(unit, s))
                    continue;
            }

            // finish off targets we can oneshot
            if (potentialTarget.GetHealth() <= unit.GetStrength())
            {
                ttk--;
            }

            // Prefer targets already in attack range, no movement needed
            if (tileHelper.IsWithinRange(unit.GetGridPos(), t, unit.GetAttackRange()))
            {
                ttk--;
            }

            //if the potential target is a secondary target add 1 to the ttk
            if (!unit.primary.Contains(tileManager.GetTileDataAt(t).GetOccupyingEntity().GetEntityType()))
            {
                ttk++;
            }

            if (ttk < bestTTK || ttk == bestTTK)
            {
                bestTTK = ttk;
                best = t;
            }
        }

        return best;
    }

    //TODO
    Vector3Int FindHard(Unit unit)
    {
        return FindMedium(unit);
    }

    //calculation for amount of turn to kill an entity, used for priority
    int TurnsToKill(List<Vector3Int> path, Unit unit, Vector3Int target)
    {
        int ttk = 0;

        //turns to get(next) to target
        ttk += TurnsToReach(path, unit);

        //checks the last spot in the path to make sure there is actually a target
        if(tileManager.GetTileDataAt(target).occupyingEntity != null)
        {
            Entity temptarget = tileManager.GetTileDataAt(target).occupyingEntity;

            //so since temptarget can be attacked on the move turn you subtrace 1 turn from
            //the amount of turns required to kill target
            ttk += temptarget.GetHealth() / unit.GetStrength() - 1;

            //if there is leftover it adds another turn
            if(temptarget.GetHealth() % unit.GetStrength() > 0)
            {
                ttk++;
            }
        }

        return ttk;
        
    }

    //turns to get(next) to target
    int TurnsToReach(List<Vector3Int> path, Unit unit)
    {
        if(unit.GetMoveRange() < 1)
        {
            return -1;
        }

        int ttr = 0;

        if(path.Count > 2)
        {
            for(int i = 1; i < path.Count - 1; i++)
            {
                ttr += tileManager.GetTileDataAt(path[i]).movementCost;
            }

            ttr = Mathf.Max(1, ttr / unit.GetMoveRange() + 1);
        }

        return ttr;
    }

    bool Win1v1()
    {
        bool ret = false;
        /*if(tileGetTileDataAt(path[i]))
        {

        }*/
        return ret;
    }

    List<Vector3Int> FindPositions(bool prime, Unit unit)
    {
        List<Vector3Int> temp = new List<Vector3Int>();
        List<Unit> tempUnits;

        //grabs the list
        tempUnits = gameManager.GetAllUnits(unit.GetTeam());

        //gets all of the entities to be targeted
        for(int i = 0; i < tempUnits.Count; i++)
        {
            //only grabs an entity if it's a primary target or secondary targeting
            if(unit.primary.Contains(tempUnits[i].GetEntityType()) || !prime)
            {
                temp.Add(tempUnits[i].GetGridPos());
            }
        }

        //only grabs crops if they're a primary target or secondary targeting
        if(unit.primary.Contains(EntityType.Crop) || !prime)
        {
            List<Crop> tempCrops = gameManager.GetAllCrops();
            for(int i = 0; i < tempCrops.Count; i++)
            {
                temp.Add(tempCrops[i].GetGridPos());
            }
        }

        return temp;
    }

    public Vector3Int FindTargetInRange(Unit unit)
    {
        Vector3Int pos = unit.GetGridPos();

        int attackRange = unit.GetAttackRange();
        //do loop to see if anything is in range
        for (int x = -attackRange; x <= attackRange; x++)
        {
            for (int y = -attackRange; y <= attackRange; y++)
            {
                // Use manhattan distance to match Attack()'s adjacency check
                if (Mathf.Abs(x) + Mathf.Abs(y) > attackRange)
                    continue;

                Vector3Int checkPos = new Vector3Int(pos.x + x, pos.y + y, 0);
                TileData tile = tileManager.GetTileDataAt(checkPos);

                if (tile == null || tile.occupyingEntity == null)
                    continue;

                Entity entity = tile.occupyingEntity;

                // Make sure it's an enemy unit and not on our team
                Unit adjacentUnit = entity as Unit;
                if (adjacentUnit != null && !unit.IsSameTeamAs(adjacentUnit))
                {
                    return checkPos;
                }

                //Check if its a crop
                if (unit.isEnemy && entity is Crop)
                    return checkPos;

                //Check if its a structure that is blocking something
                if (unit.isEnemy && entity is Structure s && IsStructureBlockingPath(unit, s))
                    return checkPos;

            }
        }

        return new Vector3Int(0, 0, -1);
    }

    //Needed to allow structures to be targeted, but only when they are in the way of units
    bool IsStructureBlockingPath(Unit unit, Structure structure)
    {
        //Structures don't block paths for flying units
        if (unit.GetCanFly())
        {
            return false;
        }

        List<Vector3Int> realTargets = FindPositions(false, unit);

        foreach (Vector3Int targetPos in realTargets)
        {
            // Get the path ignoring blocking entities
            List<Vector3Int> path = tileHelper.TilePath(unit.GetGridPos(), targetPos, unit);

            // If the structure's position appears in the path, it's blocking
            if (path.Contains(structure.GetGridPos()))
            {
                return true;
            }
        }

        return false;
    }

}
