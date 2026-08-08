using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
[CreateAssetMenu(menuName = "Actions/Attack")]
public class BasicAttackAction : EntityAction
{

    [Tooltip("knockback distance must be greater than or equal to 0")]
    public int distance = 0;

    //actually checks to see if the action can be done by caster at centerTile
    public virtual bool Action(Entity caster, Vector3Int centerTile)
    {
        TileManager manager = FindFirstObjectByType<TileManager>();
        Vector3Int direction = GetDirection(caster, centerTile);

        //checks valid targets in length
        for (int i = 0; i <= length; i++)
        {
            //checks valid targets width
            for (int j = 0; j <= width; j++)
            {
                //grabs the vector3Int for the current target tile
                Vector3Int currentTile = centerTile + direction * i;

                //gets the data of the tile
                TileData data = manager.GetTileDataAt(currentTile);

                //checks to see if there is an occupant on the tile
                if (data != null && data.HasOccupant())
                {
                    //checks if the caster and occupant are on the same team
                    if (!caster.IsSameTeam(data.GetOccupyingEntity()))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    //actually has caster preform  action on centerTile
    public virtual void PerformAt(Entity caster, Vector3Int centerTile)
    {
        //make sure the caster exists
        if (caster == null)
        {
            return;
        }

        TileManager manager = FindFirstObjectByType<TileManager>();
        Vector3Int startPos = caster.GetGridPos();

        //TODO sets the offset to just be an increment of one
        Vector3Int offset = (centerTile - startPos);

        //checks valid targets in length
        for (int i = 1; i <= length; i++)
        {
            //checks valid targets width
            for (int j = 0; j < width; j++)
            {
                //finds the target tile
                Vector3Int currentTile = startPos + offset * i;
                TileData data = manager.GetTileDataAt(currentTile);

                //if there's no width we just check the center tile
                if (j == 0)
                {
                    //if the width is actionable
                    if (data != null && data.HasOccupant())
                    {
                        Entity target = data.GetOccupyingEntity();
                        if (!caster.IsSameTeam(target))
                        {
                            Debug.Log("Attacking at " + caster.GetGridPos());
                            target.TakeDamage(caster.GetStrength(), caster.GetGridPos());
                            if (distance > 0)
                            {
                                target.KnockbackHelper(caster, distance);
                            }
                        }
                    }
                }//if we have a width we go through all the widths
                else
                {
                    Vector3Int checkTile = currentTile + new Vector3Int(offset.y * j, offset.x * j, 0);
                    data = manager.GetTileDataAt(checkTile);
                    if (data != null && data.HasOccupant())
                    {
                        Entity target = data.GetOccupyingEntity();
                        if (!target.IsSameTeam(caster))
                        {
                            Debug.Log("Attacking at " + caster.GetGridPos());
                            target.TakeDamage(caster.GetStrength(), target.GetGridPos());
                            if (distance > 0)
                            {
                                target.KnockbackHelper(caster, distance);
                            }
                        }
                    }
                    checkTile = currentTile + new Vector3Int(offset.y * j * -1, offset.x * j * -1, 0);
                    data = manager.GetTileDataAt(checkTile);
                    if (data != null && data.HasOccupant())
                    {
                        Entity target = data.GetOccupyingEntity();
                        if (!target.IsSameTeam(caster))
                        {
                            Debug.Log("Attacking at " + caster.GetGridPos());
                            target.TakeDamage(caster.GetStrength(), target.GetGridPos());
                            if (distance > 0)
                            {
                                target.KnockbackHelper(caster, distance);
                            }
                        }
                    }
                }
            }
        }
    }
}
