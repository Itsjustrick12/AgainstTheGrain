using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
[CreateAssetMenu(menuName = "Actions/Attack")]
public class BasicAttackAction : EntityAction
{

    //basically the exact same as the regular one, except we send it to AttackAction instead of Action
    //finds any valid targets for the action
    public override List<Vector3Int> GetValidTargets(Entity entity)
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
        Unit unit = entity as Unit;
        if (unit == null)
        {
            Debug.LogError("Invalid Unit");
            return targets;
        }

        int range = unit.GetAttackRange();

        //checks all four directions for targets
        for (int distx = -range; distx <= range; distx++)
        {
            for (int disty = -range; disty <= range; disty++)
            {
                //makes sure that the action is done withing the action range
                if (Mathf.Abs(distx) + Mathf.Abs(disty) > range)
                {
                    continue;
                }

                Vector3Int offset = new Vector3Int(distx, disty, 0);

                //checks valid targets in length
                for (int i = 1; i <= length; i++)
                {
                    //checks valid targets width
                    for (int j = 0; j < width; j++)
                    {
                        //so first we find the tile i length away
                        Vector3Int currentTile = startPos + offset * i;
                        TileData data = TM.GetTileDataAt(currentTile);
                        if (data == null)
                        {
                            continue;
                        }

                        //if there's no width we just check the center tile
                        if (j == 0)
                        {
                            //if the width is actionable
                            if (AttackAction(unit, currentTile))
                            {
                                targets.Add(currentTile);
                            }
                        }//if we have a width we go through all the widths
                        else
                        {
                            Vector3Int checkTile = currentTile + new Vector3Int(offset.y * j, offset.x * j, 0);
                            data = TM.GetTileDataAt(checkTile);
                            if (data != null)
                            {
                                if (AttackAction(unit, checkTile))
                                {
                                    targets.Add(currentTile);
                                }
                            }
                            checkTile = currentTile + new Vector3Int(offset.y * j * -1, offset.x * j * -1, 0);
                            data = TM.GetTileDataAt(checkTile);
                            if (data != null)
                            {
                                if (AttackAction(unit, checkTile))
                                {
                                    targets.Add(currentTile);
                                }
                            }
                        }

                    }
                }
            }
        }

        return targets;
    }

    //actually checks to see if the action can be done at position tilePos
    public virtual bool AttackAction(Unit unit, Vector3Int centerTile)
    {
        TileManager manager = FindFirstObjectByType<TileManager>();
        Vector3Int direction = GetDirection(unit, centerTile);

        //checks valid targets in length
        for (int i = 0; i <= length; i++)
        {
            //checks valid targets width
            for (int j = 0; j <= width; j++)
            {
                Vector3Int currentTile = centerTile + direction * i;

                //if (j == 0)
                //{
                    TileData data = manager.GetTileDataAt(currentTile);
                    if (data != null && data.HasOccupant())
                    {
                        Unit unitCheck = data.occupyingEntity as Unit;
                        if (unitCheck != null && !unit.IsSameTeamAs(unitCheck))
                        {
                            return true;
                        }
                    }
                //}
            }
        }
        return false;
    }

    //The unit here is the unit performing the action
    public override void PerformAt(Entity entity, Vector3Int pos)
    {
        Unit unit = entity as Unit;

        //returns if the unit doesn't exist
        if (unit == null)
        {
            return;
        }//sets the animator if it exists
        else if (unit.HasAnimator())
        {
            if (pos.x - unit.GetGridPos().x != 0)
            {
                unit.animator.SetFloat("facing", pos.x - unit.GetGridPos().x);
            }
            unit.SetAnimationTrigger(actionTrigger);
        }

        if (actionSound != null)
        {
            SoundManager.Instance.PlaySound(actionSound);
        }

        PerformAttackAt(unit, pos);
    }

    //actually preforms the Action on the tile
    public virtual void PerformAttackAt(Unit unit, Vector3Int centerTile)
    {
        //make sure unit exists
        if (unit == null)
        {
            return;
        }

        TileManager manager = FindFirstObjectByType<TileManager>();
        Vector3Int startPos = unit.GetGridPos();
        Vector3Int offset = centerTile - startPos;

        //checks valid targets in length
        for (int i = 1; i <= length; i++)
        {
            //checks valid targets width
            for (int j = 0; j < width; j++)
            {
                Vector3Int currentTile = startPos + offset * i;
                TileData data = manager.GetTileDataAt(currentTile);

                //if there's no width we just check the center tile
                if (j == 0)
                {
                    //if the width is actionable
                    if (data != null && data.HasOccupant())
                    {
                        Unit unitCheck = data.occupyingEntity as Unit;
                        if (unitCheck != null && !unit.IsSameTeamAs(unitCheck))
                        {
                            unit.ShowNumber(unit.GetStrength(), unitCheck.GetGridPos(), unit.GetGridPos().x - unit.GetGridPos().x);
                            unitCheck.TakeDamage(unit.GetStrength(), unitCheck.GetGridPos());
                        }
                    }
                }//if we have a width we go through all the widths
                else
                {
                    Vector3Int checkTile = currentTile + new Vector3Int(offset.y * j, offset.x * j, 0);
                    data = manager.GetTileDataAt(checkTile);
                    if (data != null && data.HasOccupant())
                    {
                        Unit unitCheck = data.occupyingEntity as Unit;
                        if (unitCheck != null && !unit.IsSameTeamAs(unitCheck))
                        {
                            Debug.Log("Attacking at " + unitCheck.GetGridPos());
                            unit.ShowNumber(unit.GetStrength(), unitCheck.GetGridPos(), unit.GetGridPos().x - unit.GetGridPos().x);
                            unitCheck.TakeDamage(unit.GetStrength(), unitCheck.GetGridPos());
                        }
                    }
                    checkTile = currentTile + new Vector3Int(offset.y * j * -1, offset.x * j * -1, 0);
                    data = manager.GetTileDataAt(checkTile);
                    if (data != null && data.HasOccupant())
                    {
                        Unit unitCheck = data.occupyingEntity as Unit;
                        if (unitCheck != null && !unit.IsSameTeamAs(unitCheck))
                        {
                            Debug.Log("Attacking at " + unitCheck.GetGridPos());
                            unit.ShowNumber(unit.GetStrength(), unitCheck.GetGridPos(), unit.GetGridPos().x - unit.GetGridPos().x);
                            unitCheck.TakeDamage(unit.GetStrength(), unitCheck.GetGridPos());
                        }
                    }
                }
            }
        }
    }
}
