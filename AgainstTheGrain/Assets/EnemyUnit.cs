using System.Collections.Generic;
using UnityEngine;
using System.Collections;
public class EnemyUnit : Unit
{
    public List<Vector3Int> MoveTowardsNearestUnit(bool isPlayerUnit)
    {
        if (!active)
        {
            return null;
        }

        TilemapManager manager = TilemapManager.instance;
        //call several functions from the tilemap manager to determine where to move
        //find the spot we are trying to move towards
        pos = GetGridPosition();

        //Find the location of the nearest player unit
        Vector3Int target = manager.NearestPlayerUnit(pos);

        //Debug.Log("Nearest Player is at: (" + target.x + ", " + target.x + ")");
        //find the closest adjacent tile


        //if there is nothing to move towards, return
        if (target == pos)
        {
            return new List<Vector3Int> { pos };
        }

        //Get the path to the nearest player
        List<Vector3Int> path = manager.FindPath(pos, target);


        //move to the step BEFORE the target
        path.Remove(target);
        //if theres no path to the nearest player
        if (path == null || path.Count == 0)
        {
            return new List<Vector3Int> { pos };
        }

        //find the step in the path thats farthest
        int steps = Mathf.Min(moveAmt, path.Count);

        //check the length in comparison to the amt of moves possible
        return path.GetRange(0, steps);
    }

    //use for visual output for enemies
    //highlights unit location and target location
    public IEnumerator AnimatedMove()
    {
        //highlight tile
        List<Vector3Int> steps = MoveTowardsNearestUnit(true);

        manager = TilemapManager.instance;

        if (steps != null)
        {
            foreach (Vector3Int moveTo in steps)
            {

                manager.ShowReach(pos);
                manager.ShowAvailable(moveTo);

                yield return new WaitForSeconds(0.25f);

                manager.HideInfo(pos);
                manager.HideInfo(moveTo);
                //perform the movement operation
                moveToTile(moveTo);
            }
        }
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

                    manager.ShowImpassable(idealTile);
                    yield return new WaitForSeconds(0.5f);
                    opponentTile.occupant.TakeDamage(damage);
                    manager.HideInfo(idealTile);
                }
            }

            Deactivate();

            //check if the robots have won
            yield return GameManager.instance.CheckEndState();
        }
    }
    
}
